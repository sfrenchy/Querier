import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_state.dart';

class PageLayoutBloc extends Bloc<PageLayoutEvent, PageLayoutState> {
  final ApiClient _apiClient;
  final int pageId;

  PageLayoutBloc(this._apiClient, this.pageId) : super(PageLayoutLoading()) {
    on<LoadPageLayout>(_onLoadPageLayout);
    on<SaveLayout>(_onSaveLayout);
    on<AddRow>(_onAddRow);
    on<UpdateRow>(_onUpdateRow);
    on<DeleteRow>(_onDeleteRow);
    on<ReorderRows>(_onReorderRows);
    on<AddCard>(_onAddCard);
    on<DeleteCard>(_onDeleteCard);
    on<ReorderCards>(_onReorderCards);
  }

  Future<void> _onLoadPageLayout(
    LoadPageLayout event,
    Emitter<PageLayoutState> emit,
  ) async {
    try {
      emit(PageLayoutLoading());

      // Ajout de logs pour déboguer
      print('Loading page layout...');
      final rows = await _apiClient.getDynamicRows(pageId);
      print('Rows loaded: ${rows.length}');
      print('Rows data: $rows');

      emit(PageLayoutLoaded(rows));
    } catch (e, stackTrace) {
      // Ajout du stackTrace pour plus de détails
      print('Error loading page layout: $e');
      print('Stack trace: $stackTrace');
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onSaveLayout(
    SaveLayout event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      await _apiClient.updatePageLayout(pageId, currentState.rows);
      emit(currentState.copyWith(isDirty: false));
    } catch (e) {
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onAddRow(
    AddRow event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      print('Adding new row...'); // Log de début
      final newRow = await _apiClient.createDynamicRow(
        pageId,
        event.alignment ?? MainAxisAlignment.start, // Valeur par défaut
        event.crossAlignment ?? CrossAxisAlignment.start, // Valeur par défaut
        event.spacing ?? 16.0, // Valeur par défaut
      );
      print('New row created: $newRow'); // Log de succès

      final updatedRows = [...currentState.rows, newRow];
      emit(currentState.copyWith(rows: updatedRows, isDirty: true));
      print('State updated with new row'); // Log de mise à jour d'état
    } catch (e, stackTrace) {
      print('Error adding row: $e'); // Log d'erreur
      print('Stack trace: $stackTrace');
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onUpdateRow(
    UpdateRow event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      final rowIndex = currentState.rows.indexWhere((dynamic r) {
        final typedRow = r as DynamicRow;
        return typedRow.id == event.rowId;
      });
      if (rowIndex == -1) return;

      final updatedRow = await _apiClient.updateDynamicRow(
        event.rowId,
        event.alignment,
        event.crossAlignment,
        event.spacing,
      );

      final updatedRows = [...currentState.rows];
      updatedRows[rowIndex] = updatedRow;
      emit(currentState.copyWith(rows: updatedRows, isDirty: true));
    } catch (e) {
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onDeleteRow(
    DeleteRow event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      await _apiClient.deleteDynamicRow(event.rowId);
      final updatedRows = currentState.rows.where((dynamic r) {
        final typedRow = r as DynamicRow;
        return typedRow.id != event.rowId;
      }).toList() as List<DynamicRow>;
      emit(currentState.copyWith(rows: updatedRows, isDirty: true));
    } catch (e) {
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onReorderRows(
    ReorderRows event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      await _apiClient.reorderDynamicRows(pageId, event.rowIds);
      final reorderedRows = event.rowIds
          .map((id) => currentState.rows.firstWhere((dynamic r) {
                final typedRow = r as DynamicRow;
                return typedRow.id == id;
              }))
          .toList() as List<DynamicRow>;
      emit(currentState.copyWith(rows: reorderedRows, isDirty: true));
    } catch (e) {
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onAddCard(
    AddCard event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      print('Creating new card...'); // Debug log
      final newCard = await _apiClient.createDynamicCard(
        event.rowId,
        {
          'type': event.cardType,
          'titles': {
            'en': 'New ${event.cardType}',
            'fr': 'Nouveau ${event.cardType}',
          },
        },
      );
      print('Card created successfully'); // Debug log

      // Recharger complètement le layout pour être sûr d'avoir les données à jour
      add(LoadPageLayout());
    } catch (e, stackTrace) {
      print('Error adding card: $e'); // Debug log
      print('Stack trace: $stackTrace');
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onDeleteCard(
    DeleteCard event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      await _apiClient.deleteDynamicCard(event.cardId);

      final updatedRows = currentState.rows.map((dynamic row) {
        if (row == null) return row;
        final typedRow = row as DynamicRow;
        return typedRow.copyWith(
          cards: typedRow.cards.where((c) => c.id != event.cardId).toList(),
        );
      }).toList() as List<DynamicRow>;

      emit(currentState.copyWith(rows: updatedRows, isDirty: true));
    } catch (e) {
      emit(PageLayoutError(e.toString()));
    }
  }

  Future<void> _onReorderCards(
    ReorderCards event,
    Emitter<PageLayoutState> emit,
  ) async {
    if (state is! PageLayoutLoaded) return;
    final currentState = state as PageLayoutLoaded;

    try {
      await _apiClient.reorderDynamicCards(event.rowId, event.cardIds);

      final updatedRows = currentState.rows.map((dynamic row) {
        if (row == null) return row;
        final typedRow = row as DynamicRow;
        if (typedRow.id == event.rowId) {
          final reorderedCards = event.cardIds
              .map((id) => typedRow.cards.firstWhere((c) => c.id == id))
              .toList();
          return typedRow.copyWith(cards: reorderedCards);
        }
        return typedRow;
      }).toList() as List<DynamicRow>;

      emit(currentState.copyWith(rows: updatedRows, isDirty: true));
    } catch (e) {
      emit(PageLayoutError(e.toString()));
    }
  }
}

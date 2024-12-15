import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/models/layout.dart';
import 'dynamic_page_layout_event.dart';
import 'dynamic_page_layout_state.dart';

class DynamicPageLayoutBloc
    extends Bloc<DynamicPageLayoutEvent, DynamicPageLayoutState> {
  final ApiClient _apiClient;
  Layout? _currentLayout;

  DynamicPageLayoutBloc(this._apiClient) : super(DynamicPageLayoutInitial()) {
    on<LoadPageLayout>(_onLoadPageLayout);
    on<AddRow>(_onAddRow);
    on<ReorderRows>(_onReorderRows);
    on<DeleteRow>(_onDeleteRow);
    on<UpdateRowProperties>(_onUpdateRowProperties);
    on<SaveLayout>(_onSaveLayout);
    on<AddCardToRow>(_onAddCard);
    on<DeleteCard>(_onDeleteCard);
    on<ReloadPageLayout>(_onReloadPageLayout);
  }

  Future<void> _onLoadPageLayout(
      LoadPageLayout event, Emitter<DynamicPageLayoutState> emit) async {
    emit(DynamicPageLayoutLoading());
    try {
      _currentLayout = await _apiClient.getLayout(event.pageId);
      emit(DynamicPageLayoutLoaded(_currentLayout!.rows));
    } catch (e) {
      emit(DynamicPageLayoutError(e.toString()));
    }
  }

  Future<void> _onAddRow(
      AddRow event, Emitter<DynamicPageLayoutState> emit) async {
    if (_currentLayout != null && state is DynamicPageLayoutLoaded) {
      final currentState = state as DynamicPageLayoutLoaded;
      final newRow = DynamicRow(
        id: -(_currentLayout!.rows.length + 1),
        pageId: event.pageId,
        order: currentState.rows.length + 1,
        alignment: MainAxisAlignment.start,
        crossAlignment: CrossAxisAlignment.start,
        spacing: 16.0,
        cards: const [],
      );

      _currentLayout = _currentLayout!.copyWith(
        rows: [..._currentLayout!.rows, newRow],
      );
      emit(DynamicPageLayoutLoaded(_currentLayout!.rows, isDirty: true));
    }
  }

  Future<void> _onReorderRows(
      ReorderRows event, Emitter<DynamicPageLayoutState> emit) async {
    if (_currentLayout != null) {
      final updatedRows = List<DynamicRow>.from(_currentLayout!.rows);
      for (var i = 0; i < event.rowIds.length; i++) {
        final rowIndex = updatedRows.indexWhere((r) => r.id == event.rowIds[i]);
        if (rowIndex != -1) {
          updatedRows[rowIndex] = updatedRows[rowIndex].copyWith(order: i + 1);
        }
      }

      _currentLayout = _currentLayout!.copyWith(rows: updatedRows);
      emit(DynamicPageLayoutLoaded(_currentLayout!.rows, isDirty: true));
    }
  }

  Future<void> _onDeleteRow(
      DeleteRow event, Emitter<DynamicPageLayoutState> emit) async {
    if (_currentLayout != null) {
      final updatedRows =
          _currentLayout!.rows.where((r) => r.id != event.rowId).toList();
      for (var i = 0; i < updatedRows.length; i++) {
        updatedRows[i] = updatedRows[i].copyWith(order: i + 1);
      }

      _currentLayout = _currentLayout!.copyWith(rows: updatedRows);
      emit(DynamicPageLayoutLoaded(_currentLayout!.rows, isDirty: true));
    }
  }

  Future<void> _onUpdateRowProperties(
      UpdateRowProperties event, Emitter<DynamicPageLayoutState> emit) async {
    if (_currentLayout != null) {
      final updatedRows = List<DynamicRow>.from(_currentLayout!.rows);
      final rowIndex = updatedRows.indexWhere((r) => r.id == event.rowId);
      if (rowIndex != -1) {
        updatedRows[rowIndex] = updatedRows[rowIndex].copyWith(
          alignment: event.alignment,
          crossAlignment: event.crossAlignment,
          spacing: event.spacing,
        );
      }

      _currentLayout = _currentLayout!.copyWith(rows: updatedRows);
      emit(DynamicPageLayoutLoaded(_currentLayout!.rows, isDirty: true));
    }
  }

  Future<void> _onSaveLayout(
      SaveLayout event, Emitter<DynamicPageLayoutState> emit) async {
    if (_currentLayout != null) {
      try {
        emit(DynamicPageLayoutSaving());
        
        final updatedRows = _currentLayout!.rows.map((row) {
          return row.copyWith(pageId: _currentLayout!.pageId);
        }).toList();
        
        final layoutToSave = _currentLayout!.copyWith(rows: updatedRows);
        
        await _apiClient.updateLayout(event.pageId, layoutToSave);
        emit(DynamicPageLayoutLoaded(layoutToSave.rows, isDirty: false));
      } catch (e) {
        emit(DynamicPageLayoutError(e.toString()));
      }
    }
  }

  Future<void> _onAddCard(
      AddCardToRow event, Emitter<DynamicPageLayoutState> emit) async {
    if (_currentLayout != null) {
      try {
        final updatedRows = List<DynamicRow>.from(_currentLayout!.rows);
        final rowIndex = updatedRows.indexWhere((r) => r.id == event.rowId);

        if (rowIndex != -1) {
          final row = updatedRows[rowIndex];
          final tempId = -(row.cards.length + 1);
          final newCard = PlaceholderCard(
            id: tempId,
            titles: const {'en': 'New Card', 'fr': 'Nouvelle Carte'},
            order: row.cards.length + 1,
          );

          updatedRows[rowIndex] = row.copyWith(
            pageId: _currentLayout!.pageId,
            cards: [...row.cards, newCard],
          );
          
          _currentLayout = _currentLayout!.copyWith(rows: updatedRows);
          emit(DynamicPageLayoutLoaded(_currentLayout!.rows, isDirty: true));
        }
      } catch (e) {
        emit(DynamicPageLayoutError('Failed to add card: $e'));
      }
    }
  }

  Future<void> _onDeleteCard(
      DeleteCard event, Emitter<DynamicPageLayoutState> emit) async {
    if (_currentLayout != null) {
      try {
        final updatedRows = List<DynamicRow>.from(_currentLayout!.rows);
        final rowIndex = updatedRows.indexWhere((r) => r.id == event.rowId);

        if (rowIndex != -1) {
          final row = updatedRows[rowIndex];
          final updatedCards = row.cards.where((c) => c.id != event.cardId).toList();
          updatedRows[rowIndex] = row.copyWith(cards: updatedCards);
          
          _currentLayout = _currentLayout!.copyWith(rows: updatedRows);
          emit(DynamicPageLayoutLoaded(_currentLayout!.rows, isDirty: true));
        }
      } catch (e) {
        emit(DynamicPageLayoutError('Failed to delete card: $e'));
      }
    }
  }

  Future<void> _onReloadPageLayout(
      ReloadPageLayout event, Emitter<DynamicPageLayoutState> emit) async {
    emit(DynamicPageLayoutLoading());
    try {
      _currentLayout = await _apiClient.getLayout(event.pageId);
      emit(DynamicPageLayoutLoaded(_currentLayout!.rows));
    } catch (e) {
      emit(DynamicPageLayoutError(e.toString()));
    }
  }
}

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/models/layout.dart';
import 'dynamic_page_layout_event.dart';
import 'dynamic_page_layout_state.dart';

class DynamicPageLayoutBloc
    extends Bloc<DynamicPageLayoutEvent, DynamicPageLayoutState> {
  final ApiClient _apiClient;

  DynamicPageLayoutBloc(this._apiClient) : super(DynamicPageLayoutInitial()) {
    on<LoadPageLayout>(_onLoadPageLayout);
    on<AddRow>(_onAddRow);
    on<AddCardToRow>(_onAddCard);
    on<UpdateCard>(_onUpdateCard);
    on<SaveLayout>(_onSaveLayout);
  }

  Future<void> _onUpdateCard(UpdateCard event, Emitter<DynamicPageLayoutState> emit) async {
    if (state is DynamicPageLayoutLoaded) {
      final currentState = state as DynamicPageLayoutLoaded;
      final updatedRows = currentState.rows.map((row) {
        if (row.id == event.rowId) {
          final updatedCards = row.cards.map((card) {
            if (card.id == event.card.id) {
              return event.card;
            }
            return card;
          }).toList();
          return row.copyWith(cards: updatedCards);
        }
        return row;
      }).toList();

      emit(DynamicPageLayoutLoaded(updatedRows, isDirty: true));
    }
  }

  Future<void> _onSaveLayout(SaveLayout event, Emitter<DynamicPageLayoutState> emit) async {
    if (state is DynamicPageLayoutLoaded) {
      try {
        final currentState = state as DynamicPageLayoutLoaded;
        emit(DynamicPageLayoutSaving());
        
        final layout = Layout(
          pageId: event.pageId,
          rows: currentState.rows,
          icon: 'dashboard',
          names: const {'en': 'Page Layout', 'fr': 'Mise en page'},
          isVisible: true,
          roles: const ['User'],
          route: '/layout',
        );
        
        await _apiClient.updateLayout(event.pageId, layout);
        emit(DynamicPageLayoutLoaded(currentState.rows, isDirty: false));
      } catch (e) {
        emit(DynamicPageLayoutError(e.toString()));
      }
    }
  }

  Future<void> _onLoadPageLayout(LoadPageLayout event, Emitter<DynamicPageLayoutState> emit) async {
    emit(DynamicPageLayoutLoading());
    try {
      final layout = await _apiClient.getLayout(event.pageId);
      emit(DynamicPageLayoutLoaded(layout.rows));
    } catch (e) {
      emit(DynamicPageLayoutError(e.toString()));
    }
  }

  Future<void> _onAddRow(AddRow event, Emitter<DynamicPageLayoutState> emit) async {
    if (state is DynamicPageLayoutLoaded) {
      final currentState = state as DynamicPageLayoutLoaded;
      final newRow = DynamicRow(
        id: -(currentState.rows.length + 1),
        pageId: event.pageId,
        order: currentState.rows.length + 1,
        alignment: MainAxisAlignment.start,
        crossAlignment: CrossAxisAlignment.start,
        spacing: 16.0,
        cards: const [],
      );

      final updatedRows = [...currentState.rows, newRow];
      emit(DynamicPageLayoutLoaded(updatedRows, isDirty: true));
    }
  }

  Future<void> _onAddCard(AddCardToRow event, Emitter<DynamicPageLayoutState> emit) async {
    print('_onAddCard: rowId=${event.rowId}, cardType=${event.cardType}');
    if (state is DynamicPageLayoutLoaded) {
      final currentState = state as DynamicPageLayoutLoaded;
      final row = currentState.rows.firstWhere((r) => r.id == event.rowId);
      
      DynamicCard newCard;
      print('Creating card of type: ${event.cardType}');
      switch (event.cardType) {
        case 'placeholder':
          print('Creating PlaceholderCard');
          newCard = PlaceholderCard(
            id: -(row.cards.length + 1),
            titles: const {'en': 'New Card', 'fr': 'Nouvelle Carte'},
            order: row.cards.length + 1,
            gridWidth: event.gridWidth,
          );
          break;
        case 'Table':
          print('Creating TableCard');
          newCard = TableCard(
            id: -(row.cards.length + 1),
            titles: const {'en': 'New Table', 'fr': 'Nouveau Tableau'},
            order: row.cards.length + 1,
            gridWidth: event.gridWidth,
          );
          break;
        default:
          print('Unknown card type: ${event.cardType}');
          throw Exception('Unknown card type: ${event.cardType}');
      }

      final updatedRows = currentState.rows.map((r) => 
        r.id == event.rowId 
          ? r.copyWith(cards: [...r.cards, newCard])
          : r
      ).toList();

      emit(DynamicPageLayoutLoaded(updatedRows, isDirty: true));
    }
  }
}

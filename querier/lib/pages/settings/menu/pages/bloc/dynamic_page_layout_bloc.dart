import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'dynamic_page_layout_event.dart';
import 'dynamic_page_layout_state.dart';

class DynamicPageLayoutBloc
    extends Bloc<DynamicPageLayoutEvent, DynamicPageLayoutState> {
  final ApiClient _apiClient;

  DynamicPageLayoutBloc(this._apiClient) : super(DynamicPageLayoutInitial()) {
    on<LoadPageLayout>(_onLoadPageLayout);
    on<AddRow>(_onAddRow);
    on<ReorderRows>(_onReorderRows);
  }

  Future<void> _onLoadPageLayout(
      LoadPageLayout event, Emitter<DynamicPageLayoutState> emit) async {
    emit(DynamicPageLayoutLoading());
    try {
      final rows = await _apiClient.getDynamicRows(event.pageId);
      emit(DynamicPageLayoutLoaded(rows));
    } catch (e) {
      emit(DynamicPageLayoutError(e.toString()));
    }
  }

  Future<void> _onAddRow(
      AddRow event, Emitter<DynamicPageLayoutState> emit) async {
    try {
      if (state is DynamicPageLayoutLoaded) {
        final currentState = state as DynamicPageLayoutLoaded;
        await _apiClient.createDynamicRow(event.pageId, {
          'order': currentState.rows.length + 1,
        });
        add(LoadPageLayout(event.pageId));
      }
    } catch (e) {
      emit(DynamicPageLayoutError(e.toString()));
    }
  }

  Future<void> _onReorderRows(
      ReorderRows event, Emitter<DynamicPageLayoutState> emit) async {
    try {
      await _apiClient.reorderDynamicRows(event.pageId, event.rowIds);
      add(LoadPageLayout(event.pageId));
    } catch (e) {
      emit(DynamicPageLayoutError(e.toString()));
    }
  }
}

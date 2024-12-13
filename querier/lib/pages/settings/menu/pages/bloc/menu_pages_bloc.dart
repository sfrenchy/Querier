import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/page.dart';
import 'package:querier/pages/settings/menu/pages/bloc/menu_pages_event.dart';
import 'package:querier/pages/settings/menu/pages/bloc/menu_pages_state.dart';

class MenuPagesBloc extends Bloc<MenuPagesEvent, MenuPagesState> {
  final ApiClient apiClient;

  MenuPagesBloc(this.apiClient) : super(MenuPagesInitial()) {
    on<LoadPages>(_onLoadPages);
    on<DeletePage>(_onDeletePage);
    on<UpdatePageVisibility>(_onUpdatePageVisibility);
    on<CreatePage>(_onCreatePage);
    on<UpdatePage>(_onUpdatePage);
  }

  Future<void> _onLoadPages(
      LoadPages event, Emitter<MenuPagesState> emit) async {
    emit(MenuPagesLoading());
    try {
      final pages = await apiClient.getPages(event.categoryId);
      emit(MenuPagesLoaded(pages));
    } catch (e) {
      emit(MenuPagesError(e.toString()));
    }
  }

  Future<void> _onDeletePage(
      DeletePage event, Emitter<MenuPagesState> emit) async {
    try {
      await apiClient.deletePage(event.pageId);
      add(LoadPages((state as MenuPagesLoaded).pages.first.menuCategoryId));
    } catch (e) {
      emit(MenuPagesError(e.toString()));
    }
  }

  Future<void> _onUpdatePageVisibility(
      UpdatePageVisibility event, Emitter<MenuPagesState> emit) async {
    try {
      event.page.isVisible = event.isVisible;
      await apiClient.updatePage(event.page.id, event.page);
      add(LoadPages(event.page.menuCategoryId));
    } catch (e) {
      emit(MenuPagesError(e.toString()));
    }
  }

  Future<void> _onCreatePage(
      CreatePage event, Emitter<MenuPagesState> emit) async {
    try {
      await apiClient.createPage(event.page);
      add(LoadPages(event.page.menuCategoryId));
    } catch (e) {
      emit(MenuPagesError(e.toString()));
    }
  }

  Future<void> _onUpdatePage(
      UpdatePage event, Emitter<MenuPagesState> emit) async {
    try {
      await apiClient.updatePage(event.page.id, event.page);
      add(LoadPages(event.page.menuCategoryId));
    } catch (e) {
      emit(MenuPagesError(e.toString()));
    }
  }
}

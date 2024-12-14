import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/blocs/menu_bloc.dart';
import 'package:querier/models/menu_category.dart';

part 'menu_categories_event.dart';
part 'menu_categories_state.dart';

class MenuCategoriesBloc
    extends Bloc<MenuCategoriesEvent, MenuCategoriesState> {
  final ApiClient _apiClient;
  final BuildContext context;

  MenuCategoriesBloc(this._apiClient, this.context)
      : super(MenuCategoriesInitial()) {
    on<LoadMenuCategories>(_onLoadMenuCategories);
    on<DeleteMenuCategory>(_onDeleteMenuCategory);
    on<UpdateMenuCategoryVisibility>(_onUpdateMenuCategoryVisibility);
  }

  Future<void> _onLoadMenuCategories(
    LoadMenuCategories event,
    Emitter<MenuCategoriesState> emit,
  ) async {
    emit(MenuCategoriesLoading());
    try {
      final categories = await _apiClient.getMenuCategories();
      emit(MenuCategoriesLoaded(categories));
    } catch (e) {
      emit(MenuCategoriesError(e.toString()));
    }
  }

  Future<void> _onDeleteMenuCategory(
    DeleteMenuCategory event,
    Emitter<MenuCategoriesState> emit,
  ) async {
    try {
      await _apiClient.deleteMenuCategory(event.id);
      add(LoadMenuCategories());
    } catch (e) {
      emit(MenuCategoriesError(e.toString()));
    }
  }

  Future<void> _onUpdateMenuCategoryVisibility(
    UpdateMenuCategoryVisibility event,
    Emitter<MenuCategoriesState> emit,
  ) async {
    try {
      final category = event.category;
      await _apiClient.updateMenuCategory(
          category.Id,
          category.copyWith(
            isVisible: event.isVisible,
          ));
      add(LoadMenuCategories());
      context.read<MenuBloc>().add(LoadMenu());
    } catch (e) {
      emit(MenuCategoriesError(e.toString()));
    }
  }
}
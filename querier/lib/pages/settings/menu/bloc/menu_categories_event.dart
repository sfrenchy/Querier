part of 'menu_categories_bloc.dart';

abstract class MenuCategoriesEvent extends Equatable {
  const MenuCategoriesEvent();

  @override
  List<Object> get props => [];
}

class LoadMenuCategories extends MenuCategoriesEvent {}

class DeleteMenuCategory extends MenuCategoriesEvent {
  final int id;

  const DeleteMenuCategory(this.id);

  @override
  List<Object> get props => [id];
}

class UpdateMenuCategoryVisibility extends MenuCategoriesEvent {
  final MenuCategory category;
  final bool isVisible;

  const UpdateMenuCategoryVisibility(this.category, this.isVisible);

  @override
  List<Object> get props => [category, isVisible];
}

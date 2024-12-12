part of 'menu_categories_bloc.dart';

abstract class MenuCategoriesState extends Equatable {
  const MenuCategoriesState();

  @override
  List<Object> get props => [];
}

class MenuCategoriesInitial extends MenuCategoriesState {}

class MenuCategoriesLoading extends MenuCategoriesState {}

class MenuCategoriesLoaded extends MenuCategoriesState {
  final List<MenuCategory> categories;

  const MenuCategoriesLoaded(this.categories);

  @override
  List<Object> get props => [categories];
}

class MenuCategoriesError extends MenuCategoriesState {
  final String message;

  const MenuCategoriesError(this.message);

  @override
  List<Object> get props => [message];
}

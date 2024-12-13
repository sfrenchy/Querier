import 'package:querier/models/page.dart';

abstract class MenuPagesState {}

class MenuPagesInitial extends MenuPagesState {}

class MenuPagesLoading extends MenuPagesState {}

class MenuPagesLoaded extends MenuPagesState {
  final List<MenuPage> pages;
  MenuPagesLoaded(this.pages);
}

class MenuPagesError extends MenuPagesState {
  final String message;
  MenuPagesError(this.message);
}

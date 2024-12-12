import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/menu_category.dart';

part 'menu_event.dart';
part 'menu_state.dart';

class MenuBloc extends Bloc<MenuEvent, MenuState> {
  final ApiClient _apiClient;

  MenuBloc(this._apiClient) : super(MenuInitial()) {
    on<LoadMenu>(_onLoadMenu);
  }

  Future<void> _onLoadMenu(LoadMenu event, Emitter<MenuState> emit) async {
    emit(MenuLoading());
    try {
      final categories = await _apiClient.getMenuCategories();
      emit(MenuLoaded(categories));
    } catch (e) {
      emit(MenuError(e.toString()));
    }
  }
}

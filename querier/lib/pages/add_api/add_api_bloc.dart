import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

part 'add_api_event.dart';
part 'add_api_state.dart';

class AddAPIBloc extends Bloc<AddAPIEvent, AddAPIState> {
  AddAPIBloc(super.initialState);
}

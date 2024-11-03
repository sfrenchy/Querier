import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/model/available_api_url.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  final List<AvailableApiUrl> apiUrls = [
    AvailableApiUrl(id: '1', url: 'Option 1'),
    AvailableApiUrl(id: '2', url: 'Option 2'),
    AvailableApiUrl(id: '3', url: 'Option 3'),
    AvailableApiUrl(id: '4', url: 'Option 4'),
  ];
  AvailableApiUrl? selectedApiUrl;

  LoginBloc() : super(LoginInitial()) {
    emit(DropdownOptionSelectedState(apiUrls, apiUrls.first)); // Modifié ici
    selectedApiUrl = apiUrls.first;
    _emitDropdownState();

    on<LoginButtonPressed>((event, emit) async {
      try {
        await Future.delayed(const Duration(seconds: 2));
        if (event.email == "toto" && event.password == "tutu") {
          emit(LoginSuccess());
        } else {
          emit(LoginFailure(error: "test exception"));
        }
      } catch (error) {
        emit(LoginFailure(error: error.toString()));
      }
    });

    on<ApiUrlOptionEvent>((event, emit) {
      selectedApiUrl = event.selectedApiUrl;
      print("Selected option updated to: $selectedApiUrl"); // Debugging line
      _emitDropdownState();
    });
  }

  void _emitDropdownState() {
    emit(DropdownOptionSelectedState(apiUrls, selectedApiUrl!));
  }
}

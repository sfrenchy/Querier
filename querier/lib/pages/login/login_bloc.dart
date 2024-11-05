import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/model/available_api_url.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  final List<AvailableApiUrl> apiUrls = [
    AvailableApiUrl(id: '1', url: 'https://localhost:5001/api'),
  ];
  AvailableApiUrl? selectedApiUrl;

  LoginBloc() : super(LoginInitial()) {
    emit(DropdownAvailableApiSelectedState(apiUrls, apiUrls.first));
    selectedApiUrl = apiUrls.first;
    _emitDropdownState();

    on<LoginButtonPressed>((event, emit) async {
      String url = event.apiUrl;
      String username = event.email;
      String password = event.password;
      print(
          "LoginButtonPressed - url: $url, email: $username, password: $password");
      try {
        await Future.delayed(const Duration(seconds: 2));
        if (event.email == "toto" && event.password == "tutu") {
          emit(LoginSuccess());
        } else {
          emit(const LoginFailure(error: "test exception"));
        }
      } catch (error) {
        emit(LoginFailure(error: error.toString()));
      }
    });

    on<ApiUrlChangeEvent>((event, emit) {
      selectedApiUrl = event.selectedApiUrl;
      _emitDropdownState();
    });
  }

  void _emitDropdownState() {
    emit(DropdownAvailableApiSelectedState(apiUrls, selectedApiUrl!));
  }
}

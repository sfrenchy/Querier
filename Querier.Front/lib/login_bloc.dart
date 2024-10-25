import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  LoginBloc() : super(LoginInitial()) {
    on<LoginButtonPressed>((event, emit) {
      try {
        // Replace this with your own login logic
        Future.delayed(const Duration(seconds: 2));
        if (event.email == "toto" && event.password == "tutu") {
          LoginSuccess();
        } else {
          throw Exception("test exception");
        }
      } catch (error) {
        print(LoginFailure(error: error.toString()));
      }
    });
  }
}

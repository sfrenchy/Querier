import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/api_client.dart';
import 'package:querier/model/available_api_url.dart';
import 'package:shared_preferences/shared_preferences.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  List<String> apiUrls = [];
  String? selectedApiUrl;

  LoginBloc() : super(LoginInitial()) {
    _initialize();
    emit(DropdownAvailableApiSelectedState(
        apiUrls, apiUrls.isNotEmpty ? apiUrls.first : ""));
    selectedApiUrl = apiUrls.isNotEmpty ? apiUrls.first : "";
    _emitDropdownState();

    on<LoginButtonPressed>((event, emit) async {
      String url = event.apiUrl;
      String username = event.email;
      String password = event.password;
      print(
          "LoginButtonPressed - url: $url, email: $username, password: $password");
      try {
        await Future.delayed(const Duration(seconds: 2));
        final apiClient = ApiClient(baseUrl: url);
        final success = await apiClient.signIn(username, password);
        if (success) {
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

    on<RefreshApiUrlsEvent>((event, emit) async {
      await _initialize();
    });
  }

  void _emitDropdownState() {
    emit(DropdownAvailableApiSelectedState(apiUrls, selectedApiUrl!));
  }

  Future<void> _initialize() async {
    final prefs = await SharedPreferences.getInstance();
    apiUrls = prefs.getStringList("APIURLS") ?? [];
    selectedApiUrl = apiUrls.isNotEmpty ? apiUrls.first : "";
    _emitDropdownState();
  }
}

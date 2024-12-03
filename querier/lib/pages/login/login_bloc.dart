import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:querier/api/api_client.dart';

part 'login_event.dart';
part 'login_state.dart';

class LoginBloc extends Bloc<LoginEvent, LoginState> {
  ApiClient? _apiClient;

  LoginBloc() : super(const LoginState()) {
    on<UrlChanged>(_onUrlChanged);
    on<LoginSubmitted>(_onLoginSubmitted);
    on<LoadSavedUrls>(_onLoadSavedUrls);
    on<SetLoadingState>(
        (event, emit) => emit(state.copyWith(isLoading: event.isLoading)));
    on<SetConfigurationState>((event, emit) {
      print('Handling SetConfigurationState with value: ${event.isConfigured}');
      final newState = state.copyWith(isConfigured: event.isConfigured);
      print('New state isConfigured: ${newState.isConfigured}');
      emit(newState);
    });
    on<SetError>((event, emit) => emit(state.copyWith(error: event.error)));
    on<SetAuthenticated>((event, emit) =>
        emit(state.copyWith(isAuthenticated: event.isAuthenticated)));
    on<UpdateUrlsList>((event, emit) => emit(state.copyWith(urls: event.urls)));
    on<UpdateSelectedUrl>(
        (event, emit) => emit(state.copyWith(selectedUrl: event.url)));

    add(LoadSavedUrls());
  }

  Future<void> _onUrlChanged(UrlChanged event, Emitter<LoginState> emit) async {
    _apiClient = ApiClient(event.url);

    add(const SetLoadingState(true));
    add(UpdateUrlsList([]));
    add(const SetConfigurationState(false));
    add(UpdateSelectedUrl(event.url));

    try {
      final isConfigured = await _apiClient!.isConfigured();
      print('API configured response: $isConfigured');

      final prefs = await SharedPreferences.getInstance();
      final urls = prefs.getStringList('APIURLS') ?? [];

      add(UpdateUrlsList(urls));
      add(SetConfigurationState(isConfigured));
      print('Sending SetConfigurationState with value: $isConfigured');
      add(const SetLoadingState(false));

      print('Final state - isConfigured: ${state.isConfigured}');
    } catch (e) {
      print('Error checking configuration: $e');
      add(const SetError('Failed to check API configuration'));
      add(const SetConfigurationState(false));
      add(const SetLoadingState(false));
    }
  }

  Future<void> _onLoginSubmitted(
      LoginSubmitted event, Emitter<LoginState> emit) async {
    if (state.selectedUrl.isEmpty) {
      add(const SetError('Please select an API URL'));
      return;
    }

    if (event.email.isEmpty || event.password.isEmpty) {
      add(const SetError('Please fill in all fields'));
      return;
    }

    add(const SetLoadingState(true));

    try {
      _apiClient = ApiClient(state.selectedUrl);
      final response = await _apiClient!.signIn(event.email, event.password);

      if (response.statusCode == 200 && response.data != null) {
        final token = response.data['Token'];
        if (token != null) {
          _apiClient!.setAuthToken(token.toString());
          add(const SetAuthenticated(true));
          add(const SetLoadingState(false));
        } else {
          add(const SetError('Invalid response: missing Token'));
          add(const SetLoadingState(false));
        }
      } else {
        add(const SetError('Invalid credentials'));
        add(const SetLoadingState(false));
      }
    } catch (e) {
      print('Login error: $e');
      add(SetError(e.toString()));
      add(const SetLoadingState(false));
    }
  }

  Future<void> _onLoadSavedUrls(
      LoadSavedUrls event, Emitter<LoginState> emit) async {
    final prefs = await SharedPreferences.getInstance();
    final urls = prefs.getStringList('APIURLS') ?? [];
    add(UpdateUrlsList(urls));
  }
}

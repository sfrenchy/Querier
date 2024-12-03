import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/services/wizard_service.dart';
import 'package:querier/api/api_client.dart';

part 'smtp_configuration_event.dart';
part 'smtp_configuration_state.dart';

class SmtpConfigurationBloc
    extends Bloc<SmtpConfigurationEvent, SmtpConfigurationState> {
  final WizardService _wizardService;
  final ApiClient _apiClient;

  SmtpConfigurationBloc(String baseUrl)
      : _wizardService = WizardService(baseUrl),
        _apiClient = ApiClient(baseUrl),
        super(SmtpConfigurationInitial()) {
    on<SubmitSmtpConfigurationEvent>((event, emit) async {
      emit(SmtpConfigurationLoading());
      try {
        final success = await _wizardService.setup(
          name: event.adminName,
          firstName: event.adminFirstName,
          email: event.adminEmail,
          password: event.adminPassword,
          smtpHost: event.host,
          smtpPort: int.parse(event.port),
          smtpUsername: event.username,
          smtpPassword: event.password,
          useSSL: event.useSSL,
        );

        if (success) {
          try {
            final response = await _apiClient.signIn(
              event.adminEmail,
              event.adminPassword,
            );

            if (response.statusCode == 200) {
              emit(SmtpConfigurationSuccessWithAuth(response.data));
            } else {
              emit(SmtpConfigurationSuccess());
            }
          } catch (authError) {
            emit(SmtpConfigurationSuccess());
          }
        } else {
          emit(SmtpConfigurationFailure('Setup failed'));
        }
      } catch (e) {
        emit(SmtpConfigurationFailure(e.toString()));
      }
    });
  }
}

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/services/wizard_service.dart';
import 'package:dio/dio.dart';
import 'package:querier/repositories/api_endpoints.dart';

part 'smtp_configuration_event.dart';
part 'smtp_configuration_state.dart';

class SmtpConfigurationBloc
    extends Bloc<SmtpConfigurationEvent, SmtpConfigurationState> {
  final WizardService _wizardService;
  final dio = Dio();

  SmtpConfigurationBloc(this._wizardService)
      : super(SmtpConfigurationInitial()) {
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
          // Tenter de se connecter après la configuration
          try {
            final response = await dio.post(
              ApiEndpoints.buildUrl(
                  _wizardService.baseUrl, ApiEndpoints.signIn),
              data: {
                'email': event.adminEmail,
                'password': event.adminPassword,
              },
            );

            if (response.statusCode == 200) {
              emit(SmtpConfigurationSuccessWithAuth(response.data));
            } else {
              emit(SmtpConfigurationSuccess());
            }
          } catch (authError) {
            // Si l'authentification échoue, on émet quand même un succès de configuration
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

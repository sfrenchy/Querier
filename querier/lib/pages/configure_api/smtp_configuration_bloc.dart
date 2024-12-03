import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/services/wizard_service.dart';

part 'smtp_configuration_event.dart';
part 'smtp_configuration_state.dart';

class SmtpConfigurationBloc
    extends Bloc<SmtpConfigurationEvent, SmtpConfigurationState> {
  final WizardService _wizardService;

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
          emit(SmtpConfigurationSuccess());
        } else {
          emit(SmtpConfigurationFailure('Setup failed'));
        }
      } catch (e) {
        emit(SmtpConfigurationFailure(e.toString()));
      }
    });
  }
}

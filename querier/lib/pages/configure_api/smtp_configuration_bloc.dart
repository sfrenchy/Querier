import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:equatable/equatable.dart';

part 'smtp_configuration_event.dart';
part 'smtp_configuration_state.dart';

class SmtpConfigurationBloc
    extends Bloc<SmtpConfigurationEvent, SmtpConfigurationState> {
  SmtpConfigurationBloc() : super(SmtpConfigurationInitial()) {
    on<SubmitSmtpConfigurationEvent>((event, emit) async {
      emit(SmtpConfigurationLoading());
      try {
        // TODO: Implement API call
        emit(SmtpConfigurationSuccess());
      } catch (e) {
        emit(SmtpConfigurationFailure(e.toString()));
      }
    });
  }
}

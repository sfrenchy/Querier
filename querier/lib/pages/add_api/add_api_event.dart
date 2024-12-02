part of 'add_api_bloc.dart';

abstract class AddAPIEvent extends Equatable {
  const AddAPIEvent();

  @override
  List<Object> get props => [];
}

class AddAPIProtocolChangeEvent extends AddAPIEvent {
  final String selectedProtocol;
  const AddAPIProtocolChangeEvent(this.selectedProtocol);
}

class AddAPIHostChangeEvent extends AddAPIEvent {
  final String host;
  const AddAPIHostChangeEvent(this.host);
}

class AddAPIPortChangeEvent extends AddAPIEvent {
  final int port;
  const AddAPIPortChangeEvent(this.port);
}

class AddAPIURLPathChangeEvent extends AddAPIEvent {
  final String urlPath;
  const AddAPIURLPathChangeEvent(this.urlPath);
}

class AddAPIURLChangeEvent extends AddAPIEvent {
  final String apiURl;
  const AddAPIURLChangeEvent(this.apiURl);
}

class AddAPISaveEvent extends AddAPIEvent {}

class AddAPIInitializeEvent extends AddAPIEvent {}

class CheckAPIConfigurationEvent extends AddAPIEvent {
  final String apiUrl;
  const CheckAPIConfigurationEvent(this.apiUrl);

  @override
  List<Object> get props => [apiUrl];
}

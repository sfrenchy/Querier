part of 'add_api_bloc.dart';

abstract class AddAPIState extends Equatable {
  const AddAPIState();

  @override
  List<Object> get props => [];
}

class DropdownProtocolSelectedState extends AddAPIState {
  final List<String> protocols;
  final String selectedProtocol;

  const DropdownProtocolSelectedState(this.protocols, this.selectedProtocol);

  @override
  List<Object> get props => [protocols, selectedProtocol];
}

class AddAPIInitial extends AddAPIState {
  final String host;
  final int port;
  final String urlPath;
  const AddAPIInitial(this.host, this.port, this.urlPath);
  @override
  List<Object> get props => [host, port, urlPath];
}

class AddAPIURL extends AddAPIState {
  final String apiURL;
  const AddAPIURL(this.apiURL);
  @override
  List<Object> get props => [apiURL];
}

class AddAPISaveSuccess extends AddAPIState {}

class APIConfigurationChecked extends AddAPIState {
  final bool isConfigured;

  const APIConfigurationChecked({required this.isConfigured});

  @override
  List<Object> get props => [isConfigured];
}

class APIConfigurationError extends AddAPIState {
  final String message;

  const APIConfigurationError(this.message);

  @override
  List<Object> get props => [message];
}

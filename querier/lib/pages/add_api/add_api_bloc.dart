import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:querier/pages/login/login_bloc.dart';
import 'package:shared_preferences/shared_preferences.dart';

part 'add_api_event.dart';
part 'add_api_state.dart';

class AddAPIBloc extends Bloc<AddAPIEvent, AddAPIState> {
  final List<String> protocols = ["http", "https"];
  String selectedProtocol = "http";
  String host = "localhost";
  int port = 5001;
  String urlPath = "api/v1";
  String apiUrl = "";

  AddAPIBloc() : super(const AddAPIInitial("localhost", 5001, "api/v1")) {
    // Émettre l'état initial après la configuration des valeurs initiales
    emit(AddAPIInitial(host, port, urlPath));
    Future.delayed(Duration(milliseconds: 50), () {
      _emitDropdownState();
      _computeAPIUrl();
    });

    on<AddAPIProtocolChangeEvent>((event, emit) {
      selectedProtocol = event.selectedProtocol;
      _emitDropdownState();
      _computeAPIUrl();
    });

    on<AddAPIHostChangeEvent>((event, emit) {
      host = event.host;
      _computeAPIUrl();
    });

    on<AddAPIPortChangeEvent>((event, emit) {
      port = event.port;
      _computeAPIUrl();
    });

    on<AddAPIURLPathChangeEvent>((event, emit) {
      urlPath = event.urlPath;
      _computeAPIUrl();
    });

    on<AddAPISaveEvent>((event, emit) async {
      final prefs = await SharedPreferences.getInstance();

      List<String> URLS = prefs.getStringList("APIURLS") ?? [];
      URLS.add(apiUrl);
      prefs.setStringList("APIURLS", URLS);
      emit(AddAPISaveSuccess());
    });
  }

  void _computeAPIUrl() {
    apiUrl = "$selectedProtocol://$host:$port/$urlPath";
    emit(AddAPIURL(apiUrl));
  }

  void _emitDropdownState() {
    emit(DropdownProtocolSelectedState(protocols, selectedProtocol));
  }
}

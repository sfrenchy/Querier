import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:dio/dio.dart';

part 'add_api_event.dart';
part 'add_api_state.dart';

class AddAPIBloc extends Bloc<AddAPIEvent, AddAPIState> {
  final List<String> protocols = ["http", "https"];
  String selectedProtocol = "https";
  String host = "localhost";
  int port = 5001;
  String urlPath = "api";
  String apiUrl = "";
  final dio = Dio();

  AddAPIBloc() : super(const AddAPIInitial("localhost", 5001, "api/v1")) {
    on<AddAPIInitializeEvent>((event, emit) {
      emit(AddAPIInitial(host, port, urlPath));
      _emitDropdownState(emit);
      _computeAPIUrl(emit);
    });

    // Trigger initialization
    add(AddAPIInitializeEvent());

    // Update other handlers to pass emit
    on<AddAPIProtocolChangeEvent>((event, emit) {
      selectedProtocol = event.selectedProtocol;
      _emitDropdownState(emit);
      _computeAPIUrl(emit);
      add(CheckAPIConfigurationEvent(apiUrl));
    });

    on<AddAPIHostChangeEvent>((event, emit) {
      host = event.host;
      _computeAPIUrl(emit);
    });

    on<AddAPIPortChangeEvent>((event, emit) {
      port = event.port;
      _computeAPIUrl(emit);
    });

    on<AddAPIURLPathChangeEvent>((event, emit) {
      urlPath = event.urlPath;
      _computeAPIUrl(emit);
    });

    on<AddAPISaveEvent>((event, emit) async {
      final prefs = await SharedPreferences.getInstance();

      List<String> URLS = prefs.getStringList("APIURLS") ?? [];
      if (!URLS.contains(apiUrl)) {
        URLS.add(apiUrl);
        prefs.setStringList("APIURLS", URLS);
      }
      emit(AddAPISaveSuccess());
    });

    on<CheckAPIConfigurationEvent>((event, emit) async {
      try {
        final response = await dio.get('${event.apiUrl}/settings/configured');
        if (response.statusCode == 200) {
          emit(APIConfigurationChecked(isConfigured: response.data as bool));
        } else {
          emit(
              const APIConfigurationError('Failed to check API configuration'));
        }
      } catch (e) {
        emit(const APIConfigurationError('Could not connect to API'));
      }
    });
  }

  void _computeAPIUrl(Emitter<AddAPIState> emit) {
    apiUrl = "$selectedProtocol://$host:$port/$urlPath";
    emit(AddAPIURL(apiUrl));
  }

  void _emitDropdownState(Emitter<AddAPIState> emit) {
    emit(DropdownProtocolSelectedState(protocols, selectedProtocol));
  }
}

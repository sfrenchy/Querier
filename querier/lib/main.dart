import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/const.dart';
import 'package:querier/pages/add_api/add_api_bloc.dart';
import 'package:querier/pages/login/login_screen.dart';
import 'package:querier/pages/login/login_bloc.dart';

void main() {
  runApp(const QuerierApp());
}

class QuerierApp extends StatelessWidget {
  const QuerierApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        BlocProvider<LoginBloc>(
          create: (context) => LoginBloc(),
        ),
        BlocProvider<AddAPIBloc>(
          create: (context) => AddAPIBloc(),
        ),
      ],
      child: MaterialApp(
        title: 'Querier',
        debugShowCheckedModeBanner: false,
        themeMode: ThemeMode.dark,
        theme: ThemeData(
          primaryColor: MaterialColor(
            primaryColorCode,
            <int, Color>{
              50: const Color(primaryColorCode).withOpacity(0.1),
              // autres nuances...
            },
          ),
          scaffoldBackgroundColor: const Color(0xFF171821),
          fontFamily: 'IBMPlexSans',
          brightness: Brightness.dark,
        ),
        home: const LoginScreen(),
      ),
    );
  }
}
//add connection page in this code
//add UI in different page..
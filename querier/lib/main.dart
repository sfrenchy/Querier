import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:provider/provider.dart';
import 'package:querier/const.dart';
import 'package:querier/models/role.dart';
import 'package:querier/models/user.dart';
import 'package:querier/pages/home/home_screen.dart';
import 'package:querier/pages/login/login_bloc.dart';
import 'package:querier/pages/login/login_screen.dart';
import 'package:querier/pages/add_api/add_api_bloc.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/blocs/language_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/config.dart';
import 'package:querier/pages/settings/roles/bloc/roles_bloc.dart';
import 'package:querier/pages/settings/users/setting_users_screen.dart';
import 'package:querier/pages/settings/roles/setting_roles_screen.dart';
import 'package:querier/pages/settings/services/setting_services_screen.dart';
import 'package:querier/pages/settings/users/user_form_screen.dart';
import 'package:querier/pages/settings/roles/role_form_screen.dart';
import 'package:querier/theme/theme.dart';

void main() {
  runApp(const QuerierApp());
}

class QuerierApp extends StatelessWidget {
  const QuerierApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [
        Provider<ApiClient>(
          create: (context) => ApiClient(Config.apiBaseUrl),
        ),
        BlocProvider<LoginBloc>(
          create: (context) => LoginBloc(context.read<ApiClient>()),
        ),
        BlocProvider<AddApiBloc>(
          create: (context) => AddApiBloc(),
        ),
        BlocProvider<LanguageBloc>(
          create: (context) => LanguageBloc(),
        ),
        BlocProvider<RolesBloc>(
          create: (context) => RolesBloc(context.read<ApiClient>()),
        ),
      ],
      child: BlocBuilder<LanguageBloc, Locale>(
        builder: (context, locale) {
          return MaterialApp(
            locale: locale,
            title: 'Querier',
            debugShowCheckedModeBanner: false,
            theme: QuerierTheme.darkTheme,
            home: LoginScreen(),
            routes: {
              '/home': (context) => const HomeScreen(),
              '/login': (context) => LoginScreen(),
              '/users': (context) => const SettingUsersScreen(),
              '/roles': (context) => const SettingRolesScreen(),
              '/services': (context) => const SettingServicesScreen(),
              '/users/form': (context) {
                final user =
                    ModalRoute.of(context)?.settings.arguments as User?;
                return UserFormScreen(userToEdit: user);
              },
              '/roles/form': (context) {
                final role =
                    ModalRoute.of(context)?.settings.arguments as Role?;
                return RoleFormScreen(roleToEdit: role);
              },
            },
            localizationsDelegates: const [
              AppLocalizations.delegate,
              GlobalMaterialLocalizations.delegate,
              GlobalWidgetsLocalizations.delegate,
              GlobalCupertinoLocalizations.delegate,
            ],
            supportedLocales: const [
              Locale('en'),
              Locale('fr'),
            ],
          );
        },
      ),
    );
  }
}
// Add connection page in this code
// Add UI in different pages

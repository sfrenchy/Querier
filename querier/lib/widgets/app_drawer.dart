import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/providers/auth_provider.dart';
import 'package:querier/api/api_client.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/blocs/menu_bloc.dart';
import 'package:querier/pages/settings/roles/bloc/roles_bloc.dart';

class AppDrawer extends StatelessWidget {
  const AppDrawer({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final authProvider = context.watch<AuthProvider>();
    final userRoles = authProvider.userRoles ?? [];
    final locale = Localizations.localeOf(context);
    final canManageDatabase =
        userRoles.contains('Admin') || userRoles.contains('Database Manager');
    final canManageContent =
        userRoles.contains('Admin') || userRoles.contains('Content Manager');

    return BlocBuilder<MenuBloc, MenuState>(
      builder: (context, state) {
        final menuItems = <Widget>[
          Container(
            padding: const EdgeInsets.symmetric(vertical: 12),
            width: double.infinity,
            decoration: BoxDecoration(
              color: Theme.of(context).primaryColor,
            ),
            child: SafeArea(
              child: ListTile(
                leading: Image.asset(
                  'assets/images/querier_logo_no_bg_big.png',
                  width: 40,
                ),
                title: Text(
                  'Querier',
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        color: Colors.white,
                      ),
                ),
              ),
            ),
          ),
          ListTile(
            leading: const Icon(Icons.home),
            title: Text(l10n.home),
            onTap: () {
              Navigator.pushReplacementNamed(context, '/home');
            },
          ),
        ];

        // Menus dynamiques depuis la BDD
        if (state is MenuLoaded) {
          menuItems.addAll(
            state.categories
                .where((category) =>
                    category.IsVisible &&
                    category.Roles.any((role) => userRoles.contains(role)))
                .map((category) => ListTile(
                      leading: Icon(category.getIconData()),
                      title:
                          Text(category.getLocalizedName(locale.languageCode)),
                      onTap: () {
                        Navigator.pushNamed(context, category.Route);
                      },
                    )),
          );
        }

        // Menu Databases
        if (canManageDatabase) {
          menuItems.add(
            ListTile(
              leading: const Icon(Icons.storage),
              title: Text(l10n.databases),
              onTap: () {
                Navigator.pushNamed(context, '/databases');
              },
            ),
          );
        }

        // Menu Contents
        if (canManageContent) {
          menuItems.add(
            ListTile(
              leading: const Icon(Icons.menu),
              title: Text(l10n.contents),
              onTap: () {
                Navigator.pushNamed(context, '/menu/categories');
              },
            ),
          );
        }

        // Menu Settings pour Admin
        if (userRoles.contains('Admin')) {
          menuItems.add(
            ExpansionTile(
              leading: const Icon(Icons.settings),
              title: Text(l10n.settings),
              children: [
                ListTile(
                  leading: const Icon(Icons.people),
                  title: Text(l10n.users),
                  onTap: () {
                    Navigator.pop(context);
                    Navigator.pushNamed(context, '/users');
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.security),
                  title: Text(l10n.roles),
                  onTap: () {
                    Navigator.pop(context);
                    Navigator.pushNamed(context, '/roles');
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.miscellaneous_services),
                  title: Text(l10n.services),
                  onTap: () {
                    Navigator.pop(context);
                    Navigator.pushNamed(context, '/services');
                  },
                ),
              ],
            ),
          );
        }

        // Menu Logout
        menuItems.add(
          ListTile(
            leading: const Icon(Icons.logout),
            title: Text(l10n.logout),
            onTap: () async {
              try {
                await context.read<ApiClient>().signOut();
                if (context.mounted) {
                  Navigator.pushReplacementNamed(context, '/login');
                }
              } catch (e) {
                print('Error during logout: $e');
                if (context.mounted) {
                  Navigator.pushReplacementNamed(context, '/login');
                }
              }
            },
          ),
        );

        return Drawer(
          child: ListView(
            padding: EdgeInsets.zero,
            children: menuItems,
          ),
        );
      },
    );
  }
}

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/providers/auth_provider.dart';
import 'package:querier/api/api_client.dart';

class AppDrawer extends StatelessWidget {
  const AppDrawer({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final authProvider = context.watch<AuthProvider>();
    print('Current user roles in AppDrawer: ${authProvider.userRoles}');
    final isAdmin = authProvider.userRoles.contains('Admin');
    print('isAdmin in AppDrawer: $isAdmin');
    if (isAdmin) {
      print('Showing Settings menu item because user is admin');
    }

    return Drawer(
      child: ListView(
        padding: EdgeInsets.zero,
        children: [
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
          if (authProvider.userRoles.contains('Admin') ||
              authProvider.userRoles.contains('DB Connection Manager')) ...[
            ListTile(
              leading: const Icon(Icons.storage),
              title: Text(l10n.databases),
              onTap: () {
                Navigator.pushNamed(context, '/databases');
              },
            ),
          ],
          if (isAdmin) ...[
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
          ],
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
        ],
      ),
    );
  }
}

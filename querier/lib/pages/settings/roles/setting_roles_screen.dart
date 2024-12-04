import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'bloc/roles_bloc.dart';

class SettingRolesScreen extends StatelessWidget {
  const SettingRolesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return BlocProvider(
      create: (context) =>
          RolesBloc(context.read<ApiClient>())..add(LoadRoles()),
      child: Scaffold(
        appBar: AppBar(
          title: Text(l10n.roles),
        ),
        body: BlocBuilder<RolesBloc, RolesState>(
          builder: (context, state) {
            if (state is RolesLoading) {
              return const Center(child: CircularProgressIndicator());
            }

            if (state is RolesError) {
              return Center(child: Text(state.message));
            }

            if (state is RolesLoaded) {
              return Padding(
                padding: const EdgeInsets.all(16.0),
                child: SingleChildScrollView(
                  child: SizedBox(
                    width: double.infinity,
                    child: DataTable(
                      columnSpacing: 24.0,
                      columns: [
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.name,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                      ],
                      rows: state.roles.map((role) {
                        return DataRow(cells: [
                          DataCell(Text(role.name)),
                        ]);
                      }).toList(),
                    ),
                  ),
                ),
              );
            }

            return const SizedBox();
          },
        ),
      ),
    );
  }
}

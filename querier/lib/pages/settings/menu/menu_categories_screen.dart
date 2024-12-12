import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/menu_category.dart';
import 'bloc/menu_categories_bloc.dart';

class MenuCategoriesScreen extends StatefulWidget {
  const MenuCategoriesScreen({super.key});

  @override
  State<MenuCategoriesScreen> createState() => _MenuCategoriesScreenState();
}

class _MenuCategoriesScreenState extends State<MenuCategoriesScreen> {
  @override
  void initState() {
    super.initState();
    context.read<MenuCategoriesBloc>().add(LoadMenuCategories());
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final locale = Localizations.localeOf(context);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.menuCategories),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            tooltip: l10n.addMenuCategory,
            onPressed: () async {
              final result = await Navigator.pushNamed(context, '/menu/form');
              if (result == true && mounted) {
                context.read<MenuCategoriesBloc>().add(LoadMenuCategories());
              }
            },
          ),
        ],
      ),
      body: BlocBuilder<MenuCategoriesBloc, MenuCategoriesState>(
        builder: (context, state) {
          if (state is MenuCategoriesLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is MenuCategoriesError) {
            return Center(child: Text(state.message));
          }

          if (state is MenuCategoriesLoaded) {
            return Padding(
              padding: const EdgeInsets.all(16.0),
              child: Card(
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      minWidth: MediaQuery.of(context).size.width - 32,
                    ),
                    child: DataTable(
                      columnSpacing: 24.0,
                      horizontalMargin: 24.0,
                      columns: [
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.name,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.icon,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.order,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.visibility,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                        DataColumn(
                          label: Expanded(
                            child: Text(l10n.roles,
                                style: const TextStyle(
                                    fontWeight: FontWeight.bold)),
                          ),
                        ),
                      ],
                      rows: state.categories.map((category) {
                        return DataRow(
                          cells: [
                            DataCell(Text(category
                                .getLocalizedName(locale.languageCode))),
                            DataCell(Icon(Icons.home)),
                            DataCell(Text(category.Order.toString())),
                            DataCell(
                              Switch(
                                value: category.IsVisible,
                                onChanged: (value) {
                                  context.read<MenuCategoriesBloc>().add(
                                        UpdateMenuCategoryVisibility(
                                            category, value),
                                      );
                                },
                              ),
                            ),
                            DataCell(Text(category.Roles.join(', '))),
                          ],
                          onSelectChanged: (_) async {
                            final result = await Navigator.pushNamed(
                              context,
                              '/menu/form',
                              arguments: category,
                            );
                            if (result == true && mounted) {
                              context
                                  .read<MenuCategoriesBloc>()
                                  .add(LoadMenuCategories());
                            }
                          },
                        );
                      }).toList(),
                    ),
                  ),
                ),
              ),
            );
          }

          return const SizedBox();
        },
      ),
    );
  }
}

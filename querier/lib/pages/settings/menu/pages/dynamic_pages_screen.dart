import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/blocs/menu_bloc.dart';
import 'package:querier/models/menu_category.dart';
import 'package:querier/models/page.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_pages_bloc.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_pages_event.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_pages_state.dart';
import 'package:querier/pages/settings/menu/pages/dynamic_page_form.dart';
import 'package:querier/widgets/icon_selector.dart';

class DynamicPagesScreen extends StatelessWidget {
  final MenuCategory category;

  const DynamicPagesScreen({super.key, required this.category});

  Future<void> _confirmDelete(BuildContext context, MenuPage page) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.confirmDelete),
        content: Text(l10n.confirmDeletePageMessage),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(l10n.cancel),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.delete),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      context.read<DynamicPagesBloc>().add(DeletePage(page.id));
      context.read<MenuBloc>().add(LoadMenu());
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.pages),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () => _showPageForm(context),
          ),
        ],
      ),
      body: BlocBuilder<DynamicPagesBloc, DynamicPagesState>(
        builder: (context, state) {
          if (state is DynamicPagesLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          if (state is DynamicPagesError) {
            return Center(child: Text(state.message));
          }

          if (state is DynamicPagesLoaded) {
            if (state.pages.isEmpty) {
              return Center(child: Text(l10n.noPagesYet));
            }

            return ListView.builder(
              padding: const EdgeInsets.all(8.0),
              itemCount: state.pages.length,
              itemBuilder: (context, index) {
                final page = state.pages[index];
                return Card(
                  child: ListTile(
                    leading: Icon(
                      IconSelector(
                        icon: page.icon,
                        onIconSelected: (_) {},
                      ).getIconData(page.icon),
                    ),
                    title: Text(page.getLocalizedName(
                      Localizations.localeOf(context).languageCode,
                    )),
                    subtitle: Text('${l10n.order}: ${page.order}'),
                    trailing: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Switch(
                          value: page.isVisible,
                          onChanged: (value) {
                            context.read<DynamicPagesBloc>().add(
                                  UpdatePageVisibility(page, value),
                                );
                          },
                        ),
                        IconButton(
                            icon: const Icon(Icons.dashboard),
                            tooltip: l10n.pageLayout,
                            onPressed: () => {}),
                        IconButton(
                          icon: const Icon(Icons.edit),
                          onPressed: () => _showPageForm(context, page: page),
                        ),
                        IconButton(
                          icon: const Icon(Icons.delete),
                          onPressed: () => _confirmDelete(context, page),
                        ),
                      ],
                    ),
                  ),
                );
              },
            );
          }

          return const SizedBox();
        },
      ),
    );
  }

  void _showPageForm(BuildContext context, {MenuPage? page}) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => MenuPageForm(
          apiClient: context.read<ApiClient>(),
          menuCategoryId: category.Id,
          page: page,
          onSaved: () {
            context.read<DynamicPagesBloc>().add(LoadPages(category.Id));
            context.read<MenuBloc>().add(LoadMenu());
          },
        ),
      ),
    );
  }
}

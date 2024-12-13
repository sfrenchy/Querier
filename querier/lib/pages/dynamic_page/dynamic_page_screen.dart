import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_state.dart';
import 'package:querier/widgets/app_drawer.dart';
import 'package:querier/widgets/dynamic_row_widget.dart';

class DynamicPageScreen extends StatelessWidget {
  final int pageId;

  const DynamicPageScreen({super.key, required this.pageId});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (context) => PageLayoutBloc(
        context.read<ApiClient>(),
        pageId,
      )..add(LoadPageLayout()),
      child: Scaffold(
        appBar: AppBar(
          title: const Text(
              ''), // Le titre sera mis à jour plus tard si nécessaire
        ),
        drawer: const AppDrawer(),
        body: BlocBuilder<PageLayoutBloc, PageLayoutState>(
          builder: (context, state) {
            if (state is PageLayoutLoading) {
              return const Center(child: CircularProgressIndicator());
            }

            if (state is PageLayoutLoaded) {
              return ListView.builder(
                padding: const EdgeInsets.all(16.0),
                itemCount: state.rows.length,
                itemBuilder: (context, index) {
                  return DynamicRowWidget(row: state.rows[index]);
                },
              );
            }

            if (state is PageLayoutError) {
              return Center(child: Text(state.message));
            }

            return const SizedBox();
          },
        ),
      ),
    );
  }
}

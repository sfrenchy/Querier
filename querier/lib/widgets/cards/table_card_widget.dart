import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';

class TableCardWidget extends BaseCardWidget {
  const TableCardWidget({
    super.key,
    required TableCard super.card,
    super.onEdit,
    super.onDelete,
    super.dragHandle,
  });

  @override
  Widget? buildHeader(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(8.0),
      child: Row(
        children: [
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: () {},
          ),
          IconButton(
            icon: const Icon(Icons.sort),
            onPressed: () {},
          ),
        ],
      ),
    );
  }

  @override
  Widget buildCardContent(BuildContext context) {
    return const Center(
      child: Text('Table Content'),
    );
  }

  @override
  Widget? buildFooter(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(8.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.end,
        children: [
          const Text('1-10 of 100'),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: () {},
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: () {},
          ),
        ],
      ),
    );
  }
} 
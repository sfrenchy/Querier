import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/models/cards/placeholder_card.dart';
import 'package:querier/widgets/cards/card_selector.dart';
import 'package:querier/widgets/cards/card_header.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_page_layout_event.dart';
import 'package:querier/pages/settings/menu/pages/bloc/dynamic_page_layout_bloc.dart';

class DraggableRow extends StatelessWidget {
  final DynamicRow row;
  final VoidCallback onEdit;
  final VoidCallback onDelete;
  final Function(int oldIndex, int newIndex) onReorder;
  final Function(String cardData) onAcceptCard;

  const DraggableRow({
    Key? key,
    required this.row,
    required this.onEdit,
    required this.onDelete,
    required this.onReorder,
    required this.onAcceptCard,
  }) : super(key: key);

  Widget _buildCard(BuildContext context, DynamicCard card) {
    return Draggable<int>(
      data: card.id,
      feedback: Material(
        elevation: 4,
        child: SizedBox(
          width: 300,
          child: CardSelector(
            card: card,
            onEdit: () {},
            onDelete: () {
              context.read<DynamicPageLayoutBloc>().add(
                DeleteCard(row.id, card.id),
              );
            },
          ),
        ),
      ),
      childWhenDragging: Container(
        width: 300,
        height: 200,
      ),
      child: SizedBox(
        width: 300,
        child: CardSelector(
          card: card,
          onEdit: () {},
          onDelete: () {
            context.read<DynamicPageLayoutBloc>().add(
              DeleteCard(row.id, card.id),
            );
          },
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return DragTarget<String>(
      onWillAccept: (data) => data == 'placeholder',
      onAccept: (data) => onAcceptCard(data),
      builder: (context, candidateData, rejectedData) {
        return Container(
          margin: const EdgeInsets.all(8.0),
          padding: const EdgeInsets.all(8.0),
          decoration: BoxDecoration(
            border: Border.all(
              color: candidateData.isNotEmpty 
                ? Theme.of(context).primaryColor 
                : Colors.grey.shade300,
              width: candidateData.isNotEmpty ? 2 : 1,
            ),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  IconButton(
                    icon: const Icon(Icons.edit),
                    onPressed: onEdit,
                  ),
                  IconButton(
                    icon: const Icon(Icons.delete),
                    onPressed: onDelete,
                  ),
                  const Spacer(),
                  Text('Row ${row.order}'),
                ],
              ),
              Container(
                constraints: const BoxConstraints(minHeight: 100),
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: row.cards.map((card) => _buildCard(context, card)).toList(),
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

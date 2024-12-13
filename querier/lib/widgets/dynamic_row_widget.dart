import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/widgets/cards/dynamic_card_widget.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class DynamicRowWidget extends StatelessWidget {
  final DynamicRow row;
  final bool isEditable;

  const DynamicRowWidget({
    super.key,
    required this.row,
    this.isEditable = false,
  });

  @override
  Widget build(BuildContext context) {
    return DragTarget<String>(
      onAccept: isEditable
          ? (cardType) {
              context.read<PageLayoutBloc>().add(AddCard(row.id, cardType));
            }
          : null,
      builder: (context, candidateData, rejectedData) {
        return Container(
          width: double.infinity,
          padding: const EdgeInsets.all(16.0),
          constraints: const BoxConstraints(
            minHeight: 100.0,
          ),
          decoration: isEditable
              ? BoxDecoration(
                  border: Border.all(
                    color: candidateData.isNotEmpty
                        ? Colors.blue
                        : Colors.grey.shade300,
                    width: candidateData.isNotEmpty ? 2 : 1,
                  ),
                  borderRadius: BorderRadius.circular(4.0),
                )
              : null,
          child: LayoutBuilder(
            builder: (context, constraints) {
              return Row(
                mainAxisSize: MainAxisSize.max,
                mainAxisAlignment: row.alignment,
                crossAxisAlignment: row.crossAlignment,
                children: row.cards.isEmpty
                    ? [
                        Expanded(
                          child: Center(
                            child: Text('Drop a card here'),
                          ),
                        ),
                      ]
                    : row.cards.map((card) {
                        final cardWidget = DynamicCardWidget.fromModel(
                          card,
                          context,
                          isEditable: isEditable,
                          pageLayoutBloc: context.read<PageLayoutBloc>(),
                        );

                        return card.useAvailableWidth
                            ? Expanded(
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(
                                      horizontal: 8.0),
                                  child: cardWidget,
                                ),
                              )
                            : Padding(
                                padding:
                                    const EdgeInsets.symmetric(horizontal: 8.0),
                                child: cardWidget,
                              );
                      }).toList(),
              );
            },
          ),
        );
      },
    );
  }
}

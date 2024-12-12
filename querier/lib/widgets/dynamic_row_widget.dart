import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/widgets/cards/dynamic_card_widget.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class DynamicRowWidget extends StatelessWidget {
  final DynamicRow row;

  const DynamicRowWidget({super.key, required this.row});

  @override
  Widget build(BuildContext context) {
    return DragTarget<String>(
      onAccept: (cardType) {
        context.read<PageLayoutBloc>().add(AddCard(row.id, cardType));
      },
      builder: (context, candidateData, rejectedData) {
        return Container(
          padding: const EdgeInsets.all(16.0),
          constraints: const BoxConstraints(
            minHeight: 100.0,
          ),
          decoration: BoxDecoration(
            border: Border.all(
              color:
                  candidateData.isNotEmpty ? Colors.blue : Colors.grey.shade300,
              width: candidateData.isNotEmpty ? 2 : 1,
            ),
          ),
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: IntrinsicHeight(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                mainAxisAlignment: row.alignment,
                crossAxisAlignment: row.crossAlignment,
                children: row.cards.isEmpty
                    ? [
                        Container(
                          width: 200,
                          child: const Center(
                            child: Text('Drop a card here'),
                          ),
                        ),
                      ]
                    : row.cards
                        .map((card) => Padding(
                              padding:
                                  const EdgeInsets.symmetric(horizontal: 8.0),
                              child: DynamicCardWidget.fromModel(card),
                            ))
                        .toList(),
              ),
            ),
          ),
        );
      },
    );
  }
}

import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_row.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/widgets/cards/dynamic_card_widget.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

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
    final l10n = AppLocalizations.of(context)!;

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
          child: Row(
            mainAxisAlignment: row.alignment,
            crossAxisAlignment: row.crossAlignment,
            mainAxisSize: MainAxisSize.min,
            children: row.cards.isEmpty
                ? [
                    Expanded(
                      child: Center(
                        child: Text(l10n.dropCardHere),
                      ),
                    ),
                  ]
                : row.cards.map((card) {
                    final cardWidget = DynamicCardWidget.fromModel(
                      model: card,
                      isEditable: isEditable,
                      context: context,
                    );

                    Widget wrappedCard = card.useAvailableWidth
                        ? Flexible(
                            fit: FlexFit.loose,
                            child: cardWidget,
                          )
                        : Padding(
                            padding:
                                const EdgeInsets.symmetric(horizontal: 8.0),
                            child: cardWidget,
                          );

                    if (card.useAvailableHeight) {
                      wrappedCard = Flexible(
                        fit: FlexFit.loose,
                        child: wrappedCard,
                      );
                    }

                    return wrappedCard;
                  }).toList(),
          ),
        );
      },
    );
  }
}

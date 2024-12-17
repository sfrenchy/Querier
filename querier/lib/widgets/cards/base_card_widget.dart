import 'package:flutter/material.dart';
import 'package:querier/models/cards/base_card.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/widgets/cards/card_header.dart';

abstract class BaseCardWidget extends StatelessWidget {
  final DynamicCard card;
  final VoidCallback? onEdit;
  final VoidCallback? onDelete;
  final Widget? dragHandle;
  
  // Nouveaux getters optionnels pour header/footer
  Widget? buildHeader(BuildContext context) => null;
  Widget? buildFooter(BuildContext context) => null;

  // Le contenu principal de la carte (obligatoire)
  Widget buildCardContent(BuildContext context);

  const BaseCardWidget({
    super.key,
    required this.card,
    this.onEdit,
    this.onDelete,
    this.dragHandle,
  });

  @override
  Widget build(BuildContext context) {
    print('BaseCardWidget.build: headerBackgroundColor = ${card.headerBackgroundColor}'); // Debug
    print('BaseCardWidget.build: Color value = ${card.headerBackgroundColor != null ? Color(card.headerBackgroundColor!) : null}'); // Debug
    
    return Card(
      color: card.backgroundColor != null 
        ? Color(card.backgroundColor!) 
        : null,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Header par défaut avec titre et actions
          CardHeader(
            title: card.getLocalizedTitle(
              Localizations.localeOf(context).languageCode,
            ),
            onEdit: onEdit,
            onDelete: onDelete,
            dragHandle: dragHandle,
            backgroundColor: card.headerBackgroundColor != null 
              ? Color(card.headerBackgroundColor!) 
              : null,
            textColor: card.headerTextColor != null 
              ? Color(card.headerTextColor!) 
              : null,
          ),
            
          // Contenu principal
          Flexible(
            child: buildCardContent(context),
          ),
          
          // Footer optionnel
          if (buildFooter(context) != null)
            buildFooter(context)!,
        ],
      ),
    );
  }
}

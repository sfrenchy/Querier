import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';

class FLLineChartCardConfig extends StatelessWidget {
  final DynamicCard card;
  final ValueChanged<Map<String, dynamic>> onConfigurationChanged;

  const FLLineChartCardConfig({
    super.key,
    required this.card,
    required this.onConfigurationChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Container(); // Pour l'instant vide
  }
}

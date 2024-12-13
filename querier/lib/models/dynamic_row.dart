import 'package:flutter/material.dart';
import 'package:querier/models/dynamic_card.dart';

class DynamicRow {
  final int id;
  final int pageId;
  final int order;
  final MainAxisAlignment alignment;
  final CrossAxisAlignment crossAlignment;
  final double spacing;
  final List<DynamicCard> cards;

  const DynamicRow({
    required this.id,
    required this.pageId,
    required this.order,
    this.alignment = MainAxisAlignment.start,
    this.crossAlignment = CrossAxisAlignment.start,
    this.spacing = 16.0,
    this.cards = const [],
  });

  factory DynamicRow.fromJson(Map<String, dynamic> json) {
    print('DynamicRow.fromJson input: $json');
    try {
      final row = DynamicRow(
        id: json['Id'] ?? 0,
        pageId: json['PageId'] ?? 0,
        order: json['Order'] ?? 0,
        alignment: _parseMainAxisAlignment(json['Alignment']),
        crossAlignment: _parseCrossAxisAlignment(json['CrossAlignment']),
        spacing: json['Spacing']?.toDouble() ?? 16.0,
        cards: (json['Cards'] as List?)?.map((card) {
              print('Processing card: $card');
              return DynamicCard.fromJson(card as Map<String, dynamic>);
            }).toList() ??
            const [],
      );
      print('DynamicRow created successfully: $row');
      return row;
    } catch (e, stackTrace) {
      print('Error in DynamicRow.fromJson: $e');
      print('Stack trace: $stackTrace');
      rethrow;
    }
  }

  Map<String, dynamic> toJson() => {
        'Id': id,
        'PageId': pageId,
        'Order': order,
        'Alignment': alignment.name,
        'CrossAlignment': crossAlignment.name,
        'Spacing': spacing,
        'Cards': cards.map((card) => card.toJson()).toList(),
      };

  DynamicRow copyWith({
    int? id,
    int? pageId,
    int? order,
    MainAxisAlignment? alignment,
    CrossAxisAlignment? crossAlignment,
    double? spacing,
    List<DynamicCard>? cards,
  }) {
    return DynamicRow(
      id: id ?? this.id,
      pageId: pageId ?? this.pageId,
      order: order ?? this.order,
      alignment: alignment ?? this.alignment,
      crossAlignment: crossAlignment ?? this.crossAlignment,
      spacing: spacing ?? this.spacing,
      cards: cards ?? this.cards,
    );
  }

  static MainAxisAlignment _parseMainAxisAlignment(String? value) {
    return MainAxisAlignment.values.firstWhere(
      (e) => e.name == value,
      orElse: () => MainAxisAlignment.start,
    );
  }

  static CrossAxisAlignment _parseCrossAxisAlignment(String? value) {
    return CrossAxisAlignment.values.firstWhere(
      (e) => e.name == value,
      orElse: () => CrossAxisAlignment.start,
    );
  }
}

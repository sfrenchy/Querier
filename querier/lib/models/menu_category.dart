import 'package:flutter/material.dart';

class MenuCategory {
  final int Id;
  final Map<String, String> Names;
  final String Icon;
  final int Order;
  bool IsVisible;
  final List<String> Roles;
  final String Route;

  MenuCategory({
    required this.Id,
    required this.Names,
    required this.Icon,
    required this.Order,
    required this.IsVisible,
    required this.Roles,
    required this.Route,
  });

  String getLocalizedName(String languageCode) {
    return Names[languageCode] ?? Names['en'] ?? '';
  }

  factory MenuCategory.fromJson(Map<String, dynamic> json) {
    return MenuCategory(
      Id: json['Id'] ?? json['id'] ?? 0,
      Names: Map<String, String>.from(json['Names'] ?? json['names']),
      Icon: json['Icon'] ?? json['icon'],
      Order: json['Order'] ?? json['order'],
      IsVisible: json['IsVisible'] ?? json['isVisible'],
      Roles: List<String>.from(json['Roles'] ?? json['roles']),
      Route: json['Route'] ?? json['route'],
    );
  }

  Map<String, dynamic> toJson() => {
        'Id': Id,
        'Names': Names,
        'Icon': Icon,
        'Order': Order,
        'IsVisible': IsVisible,
        'Roles': Roles,
        'Route': Route,
      };

  IconData getIconData() {
    switch (Icon) {
      case 'home':
        return Icons.home;
      case 'settings':
        return Icons.settings;
      case 'person':
        return Icons.person;
      case 'menu':
        return Icons.menu;
      default:
        return Icons.error;
    }
  }
}

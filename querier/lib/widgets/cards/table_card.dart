import 'package:flutter/material.dart';
import 'package:querier/models/card_type.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/widgets/cards/base_card.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/pages/settings/card_config/card_config_screen.dart';
import 'package:querier/widgets/cards/base_card_layout.dart';
import 'dart:math' as math;
import 'package:vtable/vtable.dart';

class PersonData {
  final int id;
  final String name;
  final int age;
  final String city;
  final String role;
  final String status;

  const PersonData({
    required this.id,
    required this.name,
    required this.age,
    required this.city,
    required this.role,
    required this.status,
  });

  static List<PersonData> generateData() {
    return [
      PersonData(
        id: 1,
        name: 'John Doe',
        age: 30,
        city: 'New York',
        role: 'Developer',
        status: 'Active',
      ),
      PersonData(
        id: 2,
        name: 'Jane Smith',
        age: 25,
        city: 'London',
        role: 'Designer',
        status: 'Active',
      ),
      PersonData(
        id: 3,
        name: 'Bob Johnson',
        age: 35,
        city: 'Paris',
        role: 'Manager',
        status: 'Inactive',
      ),
      PersonData(
        id: 4,
        name: 'Alice Brown',
        age: 28,
        city: 'Berlin',
        role: 'Developer',
        status: 'Active',
      ),
      PersonData(
        id: 5,
        name: 'Charlie Wilson',
        age: 42,
        city: 'Tokyo',
        role: 'Architect',
        status: 'Active',
      ),
      PersonData(
        id: 6,
        name: 'Emma Davis',
        age: 31,
        city: 'Sydney',
        role: 'Developer',
        status: 'Active',
      ),
      PersonData(
        id: 7,
        name: 'Michael Chen',
        age: 29,
        city: 'Singapore',
        role: 'Designer',
        status: 'Inactive',
      ),
      PersonData(
        id: 8,
        name: 'Sophie Martin',
        age: 33,
        city: 'Montreal',
        role: 'Manager',
        status: 'Active',
      ),
      PersonData(
        id: 9,
        name: 'Lucas Garcia',
        age: 27,
        city: 'Madrid',
        role: 'Developer',
        status: 'Active',
      ),
      PersonData(
        id: 10,
        name: 'Isabella Silva',
        age: 36,
        city: 'São Paulo',
        role: 'Architect',
        status: 'Inactive',
      ),
    ];
  }
}

class TableCard extends StatelessWidget {
  final String title;
  final int cardId;
  final double? height;
  final double? width;
  final bool isResizable;
  final bool isCollapsible;
  final bool isEditable;
  final PageLayoutBloc pageLayoutBloc;
  final bool useAvailableWidth;
  final bool useAvailableHeight;

  const TableCard({
    super.key,
    required this.title,
    required this.cardId,
    this.height,
    this.width,
    this.isResizable = false,
    this.isCollapsible = true,
    this.isEditable = false,
    required this.pageLayoutBloc,
    this.useAvailableWidth = false,
    this.useAvailableHeight = false,
  });

  @override
  Widget build(BuildContext context) {
    final data = PersonData.generateData();
    final inactiveStyle = TextStyle(color: Colors.grey);

    return BaseCard(
      title: title,
      cardId: cardId,
      isEditable: isEditable,
      pageLayoutBloc: pageLayoutBloc,
      onConfigurePressed: () {
        print("Opening config for TableCard");
        showDialog(
          context: context,
          builder: (context) => BlocProvider.value(
            value: pageLayoutBloc,
            child: CardConfigScreen(
              card: DynamicCard(
                id: cardId,
                titles: {'en': title, 'fr': title},
                type: CardType.Table,
                order: 0,
                isResizable: isResizable,
                isCollapsible: isCollapsible,
                height: height,
                width: width,
                useAvailableWidth: useAvailableWidth,
                useAvailableHeight: useAvailableHeight,
                configuration: {},
              ),
              cardType: 'table',
            ),
          ),
        );
      },
      child: BaseCardLayout(
        useAvailableWidth: useAvailableWidth,
        useAvailableHeight: useAvailableHeight,
        width: width,
        height: height,
        child: SizedBox(
          height: 300,
          width: 800,
          child: VTable<PersonData>(
            items: data,
            startsSorted: true,
            includeCopyToClipboardAction: false,
            columns: [
              VTableColumn(
                label: 'ID',
                width: 50,
                grow: 1,
                transformFunction: (row) => row.id.toString(),
                compareFunction: (a, b) => a.id.compareTo(b.id),
              ),
              VTableColumn(
                label: 'Name',
                width: 100,
                grow: 2,
                transformFunction: (row) => row.name,
                compareFunction: (a, b) => a.name.compareTo(b.name),
              ),
              VTableColumn(
                label: 'Age',
                width: 50,
                grow: 1,
                transformFunction: (row) => row.age.toString(),
                compareFunction: (a, b) => a.age.compareTo(b.age),
                alignment: Alignment.centerRight,
              ),
              VTableColumn(
                label: 'City',
                width: 100,
                grow: 2,
                transformFunction: (row) => row.city,
                compareFunction: (a, b) => a.city.compareTo(b.city),
              ),
              VTableColumn(
                label: 'Role',
                width: 100,
                grow: 2,
                transformFunction: (row) => row.role,
                compareFunction: (a, b) => a.role.compareTo(b.role),
              ),
              VTableColumn(
                label: 'Status',
                width: 80,
                grow: 1,
                transformFunction: (row) => row.status,
                compareFunction: (a, b) => a.status.compareTo(b.status),
                styleFunction: (row) =>
                    row.status == 'Inactive' ? inactiveStyle : null,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

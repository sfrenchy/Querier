import 'package:flutter/material.dart';
import 'package:querier/models/cards/table_card.dart';
import 'package:querier/widgets/cards/base_card_widget.dart';
import 'dart:math';

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
    // Définition des colonnes avec des noms pertinents
    final columns = [
      'ID',
      'Nom',
      'Prénom',
      'Email',
      'Département',
      'Poste',
      'Salaire',
      'Date d\'embauche',
      'Téléphone',
      'Manager',
      'Bureau',
      'Statut',
      'Expérience',
      'Projets',
      'Performance'
    ];
    
    // Données factices plus réalistes
    final random = Random();
    final departments = ['IT', 'RH', 'Finance', 'Marketing', 'Ventes', 'R&D'];
    final positions = ['Junior', 'Senior', 'Lead', 'Manager', 'Directeur'];
    final lastNames = ['Martin', 'Dubois', 'Thomas', 'Robert', 'Richard', 'Petit', 'Durand', 'Leroy'];
    final firstNames = ['Jean', 'Marie', 'Pierre', 'Sophie', 'Lucas', 'Emma', 'Louis', 'Julie'];
    
    final rows = List.generate(20, (rowIndex) {
      final lastName = lastNames[random.nextInt(lastNames.length)];
      final firstName = firstNames[random.nextInt(firstNames.length)];
      final dept = departments[random.nextInt(departments.length)];
      final position = positions[random.nextInt(positions.length)];
      
      return [
        'EMP${(10000 + rowIndex).toString()}', // ID
        lastName, // Nom
        firstName, // Prénom
        '${firstName.toLowerCase()}.${lastName.toLowerCase()}@entreprise.com', // Email
        dept, // Département
        '$position ${dept}', // Poste
        '${(45000 + random.nextInt(55000))}€', // Salaire
        '${2020 + random.nextInt(4)}-${(random.nextInt(12) + 1).toString().padLeft(2, '0')}-${(random.nextInt(28) + 1).toString().padLeft(2, '0')}', // Date d'embauche
        '06${random.nextInt(99999999).toString().padLeft(8, '0')}', // Téléphone
        '${firstNames[random.nextInt(firstNames.length)]} ${lastNames[random.nextInt(lastNames.length)]}', // Manager
        'B${random.nextInt(5) + 1}-${random.nextInt(20) + 1}', // Bureau
        ['Actif', 'En congé', 'En mission'][random.nextInt(3)], // Statut
        '${random.nextInt(15) + 1} ans', // Expérience
        random.nextInt(5) + 1, // Nombre de projets
        ['A+', 'A', 'B+', 'B', 'C'][random.nextInt(5)], // Performance
      ];
    });

    final ScrollController horizontalController = ScrollController();
    final ScrollController verticalController = ScrollController();

    return SizedBox(
      width: double.infinity,
      child: Scrollbar(
        controller: verticalController,
        thumbVisibility: true,
        trackVisibility: true,
        child: Scrollbar(
          controller: horizontalController,
          thumbVisibility: true,
          trackVisibility: true,
          notificationPredicate: (notif) => notif.depth == 1,
          child: SingleChildScrollView(
            controller: verticalController,
            child: SingleChildScrollView(
              controller: horizontalController,
              scrollDirection: Axis.horizontal,
              child: DataTable(
                columns: columns.map((column) => DataColumn(
                  label: Text(
                    column,
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                )).toList(),
                rows: rows.map((row) => DataRow(
                  cells: row.map((cell) => DataCell(Text(cell.toString()))).toList(),
                )).toList(),
              ),
            ),
          ),
        ),
      ),
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
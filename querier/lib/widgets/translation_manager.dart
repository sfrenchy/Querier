import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class TranslationManager extends StatefulWidget {
  final Map<String, TextEditingController> translations;
  final Function(Map<String, String>) onTranslationsChanged;

  const TranslationManager({
    super.key,
    required this.translations,
    required this.onTranslationsChanged,
  });

  @override
  State<TranslationManager> createState() => _TranslationManagerState();
}

class _TranslationManagerState extends State<TranslationManager> {
  late Map<String, TextEditingController> _controllers;

  @override
  void initState() {
    super.initState();
    _controllers = widget.translations;
  }

  @override
  void dispose() {
    // Ne pas disposer les contrôleurs ici car ils sont gérés par le parent
    super.dispose();
  }

  @override
  void didUpdateWidget(TranslationManager oldWidget) {
    super.didUpdateWidget(oldWidget);

    // Ne pas recréer les contrôleurs, mais mettre à jour la référence
    if (widget.translations != oldWidget.translations) {
      _controllers = widget.translations;
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Column(
      children: [
        ..._controllers.entries.map(
          (entry) => Padding(
            padding: const EdgeInsets.only(bottom: 8.0),
            child: TextFormField(
              controller: entry.value,
              decoration: InputDecoration(
                labelText: '${l10n.translatedName} (${entry.key})',
              ),
              onChanged: (value) {
                // Pas besoin d'appeler onTranslationsChanged ici car le contrôleur
                // met déjà à jour le texte
              },
            ),
          ),
        ),
      ],
    );
  }
}

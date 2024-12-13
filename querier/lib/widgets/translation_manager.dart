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
  @override
  void initState() {
    super.initState();
    _addListeners();
  }

  void _addListeners() {
    widget.translations.forEach((key, controller) {
      controller.addListener(() {
        final currentValues = Map<String, String>.from(
            widget.translations.map((k, c) => MapEntry(k, c.text)));
        widget.onTranslationsChanged(currentValues);
      });
    });
  }

  @override
  void dispose() {
    widget.translations.values.forEach((controller) {
      controller.dispose();
    });
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Column(
      children: [
        ...widget.translations.entries.map(
          (entry) => Card(
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: Row(
                children: [
                  SizedBox(
                    width: 50,
                    child: Text(entry.key.toUpperCase()),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: TextFormField(
                      controller: entry.value,
                      decoration: InputDecoration(
                        labelText: l10n.translatedName,
                      ),
                    ),
                  ),
                  IconButton(
                    icon: const Icon(Icons.delete),
                    onPressed: () {
                      setState(() {
                        widget.translations.remove(entry.key);
                        widget.onTranslationsChanged(
                          widget.translations.map((key, controller) =>
                              MapEntry(key, controller.text)),
                        );
                      });
                    },
                  ),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: 8),
        ElevatedButton.icon(
          icon: const Icon(Icons.add),
          label: Text(l10n.addTranslation),
          onPressed: () => _showAddLanguageDialog(context),
        ),
      ],
    );
  }

  void _showAddLanguageDialog(BuildContext context) {
    final availableLanguages = {'en': 'English', 'fr': 'Français'};
    final existingLanguages = widget.translations.keys.toSet();
    final newLanguages = availableLanguages.entries
        .where((e) => !existingLanguages.contains(e.key));

    if (newLanguages.isEmpty) return;

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(AppLocalizations.of(context)!.selectLanguage),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: newLanguages
              .map(
                (lang) => ListTile(
                  title: Text(lang.value),
                  onTap: () {
                    setState(() {
                      widget.translations[lang.key] = TextEditingController();
                      widget.onTranslationsChanged(
                        widget.translations.map((key, controller) =>
                            MapEntry(key, controller.text)),
                      );
                    });
                    Navigator.pop(context);
                  },
                ),
              )
              .toList(),
        ),
      ),
    );
  }
}

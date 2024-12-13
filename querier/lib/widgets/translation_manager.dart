import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class TranslationManager extends StatefulWidget {
  final Map<String, TextEditingController> translations;
  final Function(Map<String, TextEditingController>) onTranslationsChanged;

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
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.translations),
        const SizedBox(height: 8),
        ...widget.translations.entries.map(
          (entry) => Card(
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: Row(
                children: [
                  Text('${entry.key.toUpperCase()}: '),
                  Expanded(
                    child: TextField(
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
                        widget.onTranslationsChanged(widget.translations);
                      });
                    },
                  ),
                ],
              ),
            ),
          ),
        ),
        ElevatedButton.icon(
          icon: const Icon(Icons.add),
          label: Text(l10n.addTranslation),
          onPressed: () {
            final availableLanguages = ['fr', 'en']
                .where((lang) => !widget.translations.containsKey(lang))
                .toList();

            if (availableLanguages.isEmpty) {
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text(l10n.noMoreLanguagesAvailable)),
              );
              return;
            }

            showDialog(
              context: context,
              builder: (context) => AlertDialog(
                title: Text(l10n.addTranslation),
                content: DropdownButtonFormField<String>(
                  hint: Text(l10n.selectLanguage),
                  items: availableLanguages
                      .map((lang) => DropdownMenuItem(
                            value: lang,
                            child: Text(lang.toUpperCase()),
                          ))
                      .toList(),
                  onChanged: (value) {
                    if (value != null) {
                      setState(() {
                        widget.translations[value] = TextEditingController();
                        widget.onTranslationsChanged(widget.translations);
                      });
                      Navigator.pop(context);
                    }
                  },
                ),
              ),
            );
          },
        ),
      ],
    );
  }
}

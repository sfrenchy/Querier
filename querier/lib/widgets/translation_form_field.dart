import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class TranslationFormField extends StatefulWidget {
  final Map<String, String> initialTranslations;
  final Function(Map<String, String>) onChanged;
  final String? Function(Map<String, String>)? validator;

  const TranslationFormField({
    super.key,
    required this.initialTranslations,
    required this.onChanged,
    this.validator,
  });

  @override
  State<TranslationFormField> createState() => _TranslationFormFieldState();
}

class _TranslationFormFieldState extends State<TranslationFormField> {
  late Map<String, String> _translations;

  @override
  void initState() {
    super.initState();
    _translations = Map.from(widget.initialTranslations);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return FormField<Map<String, String>>(
      initialValue: _translations,
      validator: (value) => widget.validator?.call(value ?? {}),
      builder: (state) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(l10n.translations),
            const SizedBox(height: 8),
            ..._buildTranslationFields(state),
            if (state.hasError)
              Text(
                state.errorText!,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
          ],
        );
      },
    );
  }

  List<Widget> _buildTranslationFields(
      FormFieldState<Map<String, String>> state) {
    final l10n = AppLocalizations.of(context)!;
    const supportedLanguages = ['en', 'fr'];
    final List<Widget> fields = [];

    for (var lang in supportedLanguages) {
      fields.add(
        Padding(
          padding: const EdgeInsets.only(bottom: 8.0),
          child: TextFormField(
            initialValue: _translations[lang] ?? '',
            decoration: InputDecoration(
              labelText: '${l10n.translatedName} ($lang)',
            ),
            onChanged: (value) {
              setState(() {
                _translations[lang] = value;
                widget.onChanged(_translations);
                state.didChange(_translations);
              });
            },
          ),
        ),
      );
    }

    return fields;
  }
}

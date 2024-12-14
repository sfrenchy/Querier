import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/models/dynamic_card.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_bloc.dart';
import 'package:querier/pages/settings/page_layout/bloc/page_layout_event.dart';
import 'package:querier/widgets/cards/common_card_config_form.dart';

class CardConfigScreen extends StatefulWidget {
  final DynamicCard card;
  final String cardType;

  const CardConfigScreen({
    super.key,
    required this.card,
    required this.cardType,
  });

  @override
  State<CardConfigScreen> createState() => _CardConfigScreenState();
}

class _CardConfigScreenState extends State<CardConfigScreen> {
  late Map<String, String> _titles;
  late bool _isResizable;
  late bool _isCollapsible;
  late double? _height;
  late double? _width;
  late bool _useAvailableWidth;
  late bool _useAvailableHeight;
  late Map<String, dynamic> _specificConfig;
  late Color _backgroundColor;
  late Color _textColor;

  @override
  void initState() {
    super.initState();
    _titles = Map<String, String>.from(widget.card.titles);
    _isResizable = widget.card.isResizable;
    _isCollapsible = widget.card.isCollapsible;
    _height = widget.card.height;
    _width = widget.card.width;
    _useAvailableWidth = widget.card.useAvailableWidth;
    _useAvailableHeight = widget.card.useAvailableHeight;
    _specificConfig = widget.card.configuration ?? {};
    _backgroundColor = Color(widget.card.backgroundColor ?? 0xFFFFFFFF);
    _textColor = Color(widget.card.textColor ?? 0xFF000000);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.configureCard),
        actions: [
          IconButton(
            icon: const Icon(Icons.save),
            onPressed: _saveConfiguration,
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Configuration commune à toutes les cartes
            CommonCardConfigForm(
              titles: _titles,
              isResizable: _isResizable,
              isCollapsible: _isCollapsible,
              height: _height,
              width: _width,
              useAvailableWidth: _useAvailableWidth,
              useAvailableHeight: _useAvailableHeight,
              onTitlesChanged: (value) {
                print('CardConfigScreen received new titles: $value');
                setState(() => _titles = value);
              },
              onResizableChanged: (value) =>
                  setState(() => _isResizable = value),
              onCollapsibleChanged: (value) =>
                  setState(() => _isCollapsible = value),
              onHeightChanged: (value) => setState(() => _height = value),
              onWidthChanged: (value) => setState(() => _width = value),
              onUseAvailableWidthChanged: (value) =>
                  setState(() => _useAvailableWidth = value),
              onUseAvailableHeightChanged: (value) =>
                  setState(() => _useAvailableHeight = value),
              backgroundColor: _backgroundColor,
              textColor: _textColor,
              onBackgroundColorChanged: (color) =>
                  setState(() => _backgroundColor = color),
              onTextColorChanged: (color) => setState(() => _textColor = color),
            ),
            const SizedBox(height: 32),
            // Configuration spécifique selon le type de carte
            _buildSpecificConfig(),
          ],
        ),
      ),
    );
  }

  Widget _buildSpecificConfig() {
    switch (widget.cardType.toLowerCase()) {
      case 'placeholder':
        return TextFormField(
          initialValue: _specificConfig['placeholderText'] ?? '',
          decoration: const InputDecoration(
            labelText: 'Placeholder Text',
            border: OutlineInputBorder(),
          ),
          onChanged: (value) {
            setState(() {
              _specificConfig['placeholderText'] = value;
            });
          },
        );
      // Ajouter d'autres cas pour les différents types de cartes
      default:
        return const SizedBox();
    }
  }

  void _saveConfiguration() {
    print('Saving configuration...'); // Debug
    context.read<PageLayoutBloc>().add(
          UpdateCardConfiguration(
            cardId: widget.card.id,
            titles: _titles,
            isResizable: _isResizable,
            isCollapsible: _isCollapsible,
            height: _height,
            width: _width,
            type: widget.cardType,
            order: widget.card.order,
            useAvailableWidth: _useAvailableWidth,
            useAvailableHeight: _useAvailableHeight,
            configuration: _specificConfig,
            backgroundColor: _backgroundColor?.value,
            textColor: _textColor?.value,
          ),
        );

    Navigator.pop(context);
  }
}

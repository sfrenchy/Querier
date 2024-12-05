import 'package:flutter/material.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class SettingServicesScreen extends StatelessWidget {
  const SettingServicesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.services),
      ),
      body: const Center(
        child: Text('Services management screen'),
      ),
    );
  }
}
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';
import 'package:querier/api/api_client.dart';
import 'package:querier/models/role.dart';
import 'package:querier/models/user.dart';
import 'package:querier/utils/validators.dart';

class UserFormScreen extends StatefulWidget {
  final User? userToEdit;

  const UserFormScreen({super.key, this.userToEdit});

  @override
  State<UserFormScreen> createState() => _UserFormScreenState();
}

class _UserFormScreenState extends State<UserFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  bool _isLoading = false;
  List<String> _selectedRoles = [];
  List<Role> _availableRoles = [];
  bool _isLoadingRoles = true;

  @override
  void initState() {
    super.initState();
    _loadRoles();
    if (widget.userToEdit != null) {
      _emailController.text = widget.userToEdit!.email;
      _firstNameController.text = widget.userToEdit!.firstName;
      _lastNameController.text = widget.userToEdit!.lastName;
      _selectedRoles = widget.userToEdit!.selectedRoles;
    }
  }

  Future<void> _loadRoles() async {
    try {
      final roles = await context.read<ApiClient>().getAllRoles();
      setState(() {
        _availableRoles = roles;
        _isLoadingRoles = false;
      });
    } catch (e) {
      // Gérer l'erreur
      setState(() => _isLoadingRoles = false);
    }
  }

  @override
  void dispose() {
    _emailController.dispose();
    _firstNameController.dispose();
    _lastNameController.dispose();
    super.dispose();
  }

  Future<void> _saveUser() async {
    final l10n = AppLocalizations.of(context)!;
    if (!_formKey.currentState!.validate()) return;

    setState(() => _isLoading = true);

    try {
      final apiClient = context.read<ApiClient>();
      bool success;

      if (widget.userToEdit != null) {
        success = await apiClient.updateUser(
          widget.userToEdit!.id,
          _emailController.text,
          _firstNameController.text,
          _lastNameController.text,
          _selectedRoles,
        );
      } else {
        success = await apiClient.addUser(
          _emailController.text,
          _firstNameController.text,
          _lastNameController.text,
          '',
          _selectedRoles,
        );
      }

      if (!mounted) return;

      if (success) {
        Navigator.pop(context, true);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(l10n.errorSavingUser)),
        );
      }
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(e.toString())),
      );
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _showDeleteConfirmation(BuildContext context) async {
    final l10n = AppLocalizations.of(context)!;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(l10n.deleteUserTitle),
        content: Text(l10n.deleteUserConfirmation),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(l10n.cancel),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(l10n.delete),
          ),
        ],
      ),
    );

    if (!mounted) return;

    if (confirmed == true) {
      setState(() => _isLoading = true);
      try {
        final success =
            await context.read<ApiClient>().deleteUser(widget.userToEdit!.id);
        if (!mounted) return;

        if (success) {
          Navigator.pop(context, true);
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(l10n.errorDeletingUser)),
          );
        }
      } catch (e) {
        if (!mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString())),
        );
      } finally {
        if (mounted) {
          setState(() => _isLoading = false);
        }
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.userToEdit != null ? l10n.editUser : l10n.addUser),
        actions: [
          if (widget.userToEdit != null)
            IconButton(
              icon: const Icon(Icons.delete),
              tooltip: l10n.deleteUser,
              onPressed: () => _showDeleteConfirmation(context),
            ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextFormField(
                controller: _emailController,
                decoration: InputDecoration(
                  labelText: l10n.email,
                  border: const OutlineInputBorder(),
                ),
                enabled: widget.userToEdit == null,
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return l10n.emailRequired;
                  }
                  if (!Validators.isValidEmail(value)) {
                    return l10n.validEmail;
                  }
                  return null;
                },
              ),
              if (widget.userToEdit != null) ...[
                const SizedBox(height: 8),
                Row(
                  children: [
                    Icon(
                      widget.userToEdit!.isEmailConfirmed
                          ? Icons.check_circle
                          : Icons.error_outline,
                      color: widget.userToEdit!.isEmailConfirmed
                          ? Colors.green
                          : Colors.orange,
                    ),
                    const SizedBox(width: 8),
                    Text(
                      widget.userToEdit!.isEmailConfirmed
                          ? l10n.emailVerified
                          : l10n.emailNotVerified,
                      style: TextStyle(
                        color: widget.userToEdit!.isEmailConfirmed
                            ? Colors.green
                            : Colors.orange,
                      ),
                    ),
                    if (!widget.userToEdit!.isEmailConfirmed) ...[
                      const Spacer(),
                      TextButton.icon(
                        onPressed: () {
                          // TODO: Implémenter la logique de renvoi
                        },
                        icon: const Icon(Icons.send),
                        label: Text(l10n.resendVerificationEmail),
                      ),
                    ],
                  ],
                ),
              ],
              const SizedBox(height: 16),
              TextFormField(
                controller: _firstNameController,
                decoration: InputDecoration(
                  labelText: l10n.firstName,
                  border: const OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return l10n.firstNameRequired;
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _lastNameController,
                decoration: InputDecoration(
                  labelText: l10n.name,
                  border: const OutlineInputBorder(),
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return l10n.nameRequired;
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              if (!_isLoadingRoles) ...[
                const SizedBox(height: 16),
                Card(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Padding(
                        padding: const EdgeInsets.all(16.0),
                        child: Text(
                          l10n.roles,
                          style: Theme.of(context).textTheme.titleMedium,
                        ),
                      ),
                      ...(_availableRoles.map((role) => CheckboxListTile(
                            title: Text(role.name),
                            value: _selectedRoles.contains(role.name),
                            onChanged: (bool? value) {
                              setState(() {
                                if (value == true) {
                                  _selectedRoles.add(role.name);
                                } else {
                                  _selectedRoles.remove(role.name);
                                }
                              });
                            },
                          ))),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 16),
              ElevatedButton(
                onPressed: _isLoading ? null : _saveUser,
                child: _isLoading
                    ? const CircularProgressIndicator()
                    : Text(l10n.save),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
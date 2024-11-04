import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:querier/pages/add_api/add_api_bloc.dart';

class AddAPIScreen extends StatefulWidget {
  const AddAPIScreen({super.key});

  @override
  _AddAPIScreenState createState() => _AddAPIScreenState();
}

class _AddAPIScreenState extends State<AddAPIScreen> {
  @override
  void dispose() {
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Add API Screen'),
      ),
      body: BlocListener<AddAPIBloc, AddAPIState>(
        listener: (context, state) {},
        child: BlocBuilder<AddAPIBloc, AddAPIState>(
          builder: (context, state) {
            return const Padding(
              padding: EdgeInsets.all(16.0),
            );
          },
        ),
      ),
    );
  }
}

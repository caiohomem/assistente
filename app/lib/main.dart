import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import 'src/auth/auth_controller.dart';
import 'src/ui/home_page.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();

  runApp(
    ChangeNotifierProvider(
      create: (_) => AuthController(),
      child: const AssistenteExecutivoApp(),
    ),
  );
}

class AssistenteExecutivoApp extends StatelessWidget {
  const AssistenteExecutivoApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Assistente Executivo',
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.indigo),
        useMaterial3: true,
      ),
      home: const HomePage(),
    );
  }
}



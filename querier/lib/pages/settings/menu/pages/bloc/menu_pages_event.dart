import 'package:querier/models/page.dart';

abstract class MenuPagesEvent {}

class LoadPages extends MenuPagesEvent {
  final int categoryId;
  LoadPages(this.categoryId);
}

class DeletePage extends MenuPagesEvent {
  final int pageId;
  DeletePage(this.pageId);
}

class CreatePage extends MenuPagesEvent {
  final MenuPage page;
  CreatePage(this.page);
}

class UpdatePage extends MenuPagesEvent {
  final MenuPage page;
  UpdatePage(this.page);
}

class UpdatePageVisibility extends MenuPagesEvent {
  final MenuPage page;
  final bool isVisible;
  UpdatePageVisibility(this.page, this.isVisible);
}

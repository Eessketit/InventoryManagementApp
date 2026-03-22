# -*- coding: utf-8 -*-
{
    'name': 'Inventory Importer',
    'version': '1.0.0',
    'category': 'Tools',
    'summary': 'Import aggregated inventory data from the Inventory Management App via API token.',
    'description': """
        Allows importing inventory statistics (fields, aggregated values) from an external
        Inventory Management application using a per-inventory API token.

        Features:
        - Import inventory title, description, category, and item count.
        - Import field definitions (type, slot, label).
        - Import field-level aggregated statistics (avg/min/max for numbers, top-5 for text, etc.).
        - Read-only viewer — no creation or modification of imported data.
        - Re-import updates existing records.
    """,
    'author': 'InventoryApp Integration',
    'depends': ['base'],
    'data': [
        'security/ir.model.access.csv',
        'views/inventory_views.xml',
        'views/wizard_views.xml',
        'views/menu.xml',
    ],
    'installable': True,
    'application': True,
    'license': 'LGPL-3',
}

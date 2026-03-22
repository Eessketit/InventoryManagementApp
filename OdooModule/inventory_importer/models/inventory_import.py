# -*- coding: utf-8 -*-
from odoo import models, fields


class InventoryImport(models.Model):
    """Stores an imported inventory from the external Inventory Management App."""
    _name = 'inventory.import'
    _description = 'Imported Inventory'
    _order = 'imported_at desc'

    name = fields.Char(string='Title', required=True, readonly=True)
    description = fields.Text(string='Description', readonly=True)
    category = fields.Char(string='Category', readonly=True)
    is_public = fields.Boolean(string='Public', readonly=True)
    item_count = fields.Integer(string='Item Count', readonly=True)
    api_token = fields.Char(string='API Token', required=True, readonly=True)
    api_url = fields.Char(string='API URL (source)', readonly=True)
    imported_at = fields.Datetime(string='Last Imported', readonly=True)
    created_at = fields.Char(string='Created At (source)', readonly=True)

    field_ids = fields.One2many(
        'inventory.field.import', 'inventory_id',
        string='Fields', readonly=True
    )


class InventoryFieldImport(models.Model):
    """Stores one field definition imported from the external app."""
    _name = 'inventory.field.import'
    _description = 'Imported Inventory Field'
    _order = 'slot'

    inventory_id = fields.Many2one(
        'inventory.import', string='Inventory',
        required=True, ondelete='cascade', readonly=True
    )
    name = fields.Char(string='Field Name', required=True, readonly=True)
    field_type = fields.Char(string='Type', readonly=True)
    slot = fields.Integer(string='Slot', readonly=True)
    in_table = fields.Boolean(string='Shown in Table', readonly=True)

    stat_ids = fields.One2many(
        'inventory.field.stat', 'field_id',
        string='Statistics', readonly=True
    )


class InventoryFieldStat(models.Model):
    """Key-value pairs for aggregated statistics of a field."""
    _name = 'inventory.field.stat'
    _description = 'Field Statistic'
    _order = 'stat_key'

    field_id = fields.Many2one(
        'inventory.field.import', string='Field',
        required=True, ondelete='cascade', readonly=True
    )
    stat_key = fields.Char(string='Key', readonly=True)
    stat_value = fields.Char(string='Value', readonly=True)

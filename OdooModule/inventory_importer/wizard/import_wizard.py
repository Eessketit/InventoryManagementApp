# -*- coding: utf-8 -*-
import requests
import json
from datetime import datetime
from odoo import models, fields, api
from odoo.exceptions import UserError


class InventoryImportWizard(models.TransientModel):
    """Wizard that fetches inventory data from the external API and upserts it."""
    _name = 'inventory.import.wizard'
    _description = 'Import Inventory from API Token'

    api_url = fields.Char(
        string='API URL',
        required=True,
        help='Full URL with token, e.g. https://yourapp.onrender.com/Api/InventoryData?token=abc123'
    )

    def action_import(self):
        """Fetch data from the API URL and upsert inventory + fields + stats."""
        self.ensure_one()

        # ── 1. HTTP Request ──────────────────────────────────────────────────
        try:
            response = requests.get(self.api_url.strip(), timeout=15)
            response.raise_for_status()
            data = response.json()
        except requests.exceptions.RequestException as e:
            raise UserError(f'Failed to fetch data from API:\n{e}')
        except Exception as e:
            raise UserError(f'Invalid JSON response from API:\n{e}')

        if 'error' in data:
            raise UserError(f'API returned error: {data["error"]}')

        # ── 2. Extract token from URL ────────────────────────────────────────
        token = ''
        if '?token=' in self.api_url:
            token = self.api_url.split('?token=')[-1].split('&')[0]
        elif '&token=' in self.api_url:
            token = self.api_url.split('&token=')[-1].split('&')[0]

        if not token:
            raise UserError('Could not extract token from URL. Make sure the URL contains ?token=...')

        # ── 3. Upsert Inventory record ───────────────────────────────────────
        InventoryModel = self.env['inventory.import']
        existing = InventoryModel.search([('api_token', '=', token)], limit=1)

        vals = {
            'name':        data.get('title', '(No Title)'),
            'description': data.get('description', ''),
            'category':    data.get('category') or '',
            'is_public':   data.get('isPublic', False),
            'item_count':  data.get('itemCount', 0),
            'api_token':   token,
            'api_url':     self.api_url.strip(),
            'imported_at': datetime.utcnow(),
            'created_at':  str(data.get('createdAt', '')),
        }

        if existing:
            existing.write(vals)
            inventory = existing
        else:
            inventory = InventoryModel.create(vals)

        # ── 4. Delete old field records and re-create ────────────────────────
        inventory.field_ids.mapped('stat_ids').unlink()
        inventory.field_ids.unlink()

        FieldModel = self.env['inventory.field.import']
        StatModel  = self.env['inventory.field.stat']

        for field_data in data.get('fields', []):
            field_rec = FieldModel.create({
                'inventory_id': inventory.id,
                'name':         field_data.get('title', ''),
                'field_type':   field_data.get('type', ''),
                'slot':         field_data.get('slot', 0),
                'in_table':     field_data.get('inTable', True),
            })

            # Parse stats dict and flatten to key-value rows
            stats = field_data.get('stats', {})
            self._create_stat_rows(StatModel, field_rec.id, stats)

        # ── 5. Return action to open the imported record ─────────────────────
        return {
            'type':      'ir.actions.act_window',
            'name':      'Imported Inventory',
            'res_model': 'inventory.import',
            'view_mode': 'form',
            'res_id':    inventory.id,
            'target':    'current',
        }

    def _create_stat_rows(self, StatModel, field_id, stats, prefix=''):
        """Recursively flatten nested stats dict into key-value rows."""
        for key, value in stats.items():
            full_key = f'{prefix}{key}' if prefix else key
            if isinstance(value, dict):
                self._create_stat_rows(StatModel, field_id, value, prefix=f'{full_key}.')
            elif isinstance(value, list):
                # top-5 list: each item is {value: ..., count: ...}
                for i, item in enumerate(value):
                    if isinstance(item, dict):
                        for sub_key, sub_val in item.items():
                            StatModel.create({
                                'field_id':   field_id,
                                'stat_key':   f'{full_key}[{i}].{sub_key}',
                                'stat_value': str(sub_val),
                            })
                    else:
                        StatModel.create({
                            'field_id':   field_id,
                            'stat_key':   f'{full_key}[{i}]',
                            'stat_value': str(item),
                        })
            else:
                StatModel.create({
                    'field_id':   field_id,
                    'stat_key':   full_key,
                    'stat_value': str(value),
                })

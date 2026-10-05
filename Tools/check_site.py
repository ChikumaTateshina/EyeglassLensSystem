from html.parser import HTMLParser
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
class Links(HTMLParser):
    def __init__(self):
        super().__init__(); self.ids = set(); self.refs = []; self.titles = 0
    def handle_starttag(self, tag, attributes):
        values = dict(attributes)
        if 'id' in values:
            assert values['id'] not in self.ids, f'Duplicate id: {values["id"]}'
            self.ids.add(values['id'])
        for field in ('href', 'src'):
            if field in values: self.refs.append(values[field])
        if tag == 'title': self.titles += 1

html = (root/'docs/index.html').read_text(encoding='utf-8')
page = Links(); page.feed(html)
for ref in page.refs:
    if ref.startswith('#') and len(ref) > 1: assert ref[1:] in page.ids, f'Missing anchor: {ref}'
    elif not re.match(r'^(https?:|mailto:|#|data:)', ref): assert (root/'docs'/ref).is_file(), f'Missing file: {ref}'
assert '<html lang="ja">' in html and 'name="viewport"' in html
assert 'Read/Write Enabled' in html and '3030' in html and '3020' in html
assert page.titles >= 1
assert (root/'LICENSE').read_text().startswith('MIT License')
for path in (root/'Assets/PrescriptionLens').rglob('*'):
    if path.is_file() and path.suffix != '.meta': assert Path(str(path)+'.meta').is_file(), f'Missing Unity meta: {path}'
assert not list((root/'Assets').rglob('*.fbx')), 'Personal models must not be distributed'
print(f'PASS website: {len(page.ids)} anchors, {len(page.refs)} references; package metadata and license')

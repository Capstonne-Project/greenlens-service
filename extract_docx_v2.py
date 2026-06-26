"""Extract .docx to clean UTF-8 markdown, preserving Vietnamese text and tables."""
import sys
from pathlib import Path
from docx import Document
from docx.table import Table
from docx.text.paragraph import Paragraph
from docx.oxml.ns import qn


def iter_block_items(parent):
    """Yield Paragraph and Table objects in document order."""
    body = parent.element.body if hasattr(parent, 'element') else parent
    for child in body.iterchildren():
        if child.tag == qn('w:p'):
            yield Paragraph(child, parent)
        elif child.tag == qn('w:tbl'):
            yield Table(child, parent)


def table_to_markdown(table: Table) -> str:
    """Convert a docx Table to markdown table format."""
    rows = []
    for row in table.rows:
        cells = [cell.text.strip().replace('\n', ' ') for cell in row.cells]
        rows.append(cells)

    if not rows:
        return ''

    # Build markdown table
    lines = []
    # Header
    lines.append('| ' + ' | '.join(rows[0]) + ' |')
    lines.append('|' + '|'.join(['---'] * len(rows[0])) + '|')
    # Data rows
    for row in rows[1:]:
        # Pad if fewer cells
        while len(row) < len(rows[0]):
            row.append('')
        lines.append('| ' + ' | '.join(row[:len(rows[0])]) + ' |')

    return '\n'.join(lines)


def para_to_markdown(para: Paragraph) -> str:
    """Convert a paragraph to markdown, respecting heading styles."""
    text = para.text.strip()
    if not text:
        return ''

    style_name = (para.style.name if para.style else '').lower()

    if 'heading 1' in style_name:
        return f'# {text}'
    elif 'heading 2' in style_name:
        return f'## {text}'
    elif 'heading 3' in style_name:
        return f'### {text}'
    elif 'heading 4' in style_name:
        return f'#### {text}'
    elif 'list' in style_name:
        return f'- {text}'
    else:
        return text


def extract_docx(input_path: str, output_path: str):
    doc = Document(input_path)
    lines = []

    for block in iter_block_items(doc):
        if isinstance(block, Paragraph):
            md = para_to_markdown(block)
            if md:
                lines.append(md)
                lines.append('')  # blank line after paragraph
        elif isinstance(block, Table):
            lines.append(table_to_markdown(block))
            lines.append('')  # blank line after table

    content = '\n'.join(lines)

    with open(output_path, 'w', encoding='utf-8') as f:
        f.write(content)

    print(f'OK - Extracted {len(lines)} lines to {output_path}')
    print(f'   File size: {Path(output_path).stat().st_size:,} bytes (UTF-8)')


if __name__ == '__main__':
    if len(sys.argv) < 3:
        print('Usage: python extract_docx_v2.py <input.docx> <output.md>')
        sys.exit(1)
    extract_docx(sys.argv[1], sys.argv[2])

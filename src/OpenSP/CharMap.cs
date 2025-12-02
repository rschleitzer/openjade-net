// Copyright (c) 1997 James Clark, 2000 Matthias Clasen
// See the file COPYING for copying permission.

namespace OpenSP;

// SP_MULTI_BYTE is always defined for this port (multi-byte character support)

public static class CharMapBits
{
    // 21 bits are enough for the UTF-16 range
    public const int level0 = 5;
    public const int level1 = 8;
    public const int level2 = 4;
    public const int level3 = 4;

    public const int planes = (1 << level0);
    public const int pagesPerPlane = (1 << level1);
    public const int columnsPerPage = (1 << level2);
    public const int cellsPerColumn = (1 << level3);
    public const int planeSize = (1 << (level1 + level2 + level3));
    public const int pageSize = (1 << (level2 + level3));
    public const int columnSize = (1 << level3);

    public static nuint planeIndex(nuint c)
    {
        return (c >> (level1 + level2 + level3));
    }

    public static nuint pageIndex(nuint c)
    {
        return ((c >> (level2 + level3)) & (pagesPerPlane - 1));
    }

    public static nuint columnIndex(nuint c)
    {
        return ((c >> level3) & (columnsPerPage - 1));
    }

    public static nuint cellIndex(nuint c)
    {
        return (c & (cellsPerColumn - 1));
    }

    public static nuint maxInPlane(nuint c)
    {
        return (c | (planeSize - 1));
    }

    public static nuint maxInPage(nuint c)
    {
        return (c | (pageSize - 1));
    }

    public static nuint maxInColumn(nuint c)
    {
        return (c | (columnSize - 1));
    }
}

public class CharMapColumn<T>
{
    public T[]? values;
    public T value = default!;

    public CharMapColumn()
    {
        values = null;
    }

    public CharMapColumn(CharMapColumn<T> col)
    {
        if (col.values != null)
        {
            values = new T[CharMapBits.cellsPerColumn];
            for (int i = 0; i < CharMapBits.cellsPerColumn; i++)
                values[i] = col.values[i];
        }
        else
        {
            values = null;
            value = col.value;
        }
    }

    public void operatorAssign(CharMapColumn<T> col)
    {
        if (col.values != null)
        {
            if (values == null)
                values = new T[CharMapBits.cellsPerColumn];
            for (int i = 0; i < CharMapBits.cellsPerColumn; i++)
                values[i] = col.values[i];
        }
        else
        {
            values = null;
            value = col.value;
        }
    }
}

public class CharMapPage<T>
{
    public CharMapColumn<T>[]? values;
    public T value = default!;

    public CharMapPage()
    {
        values = null;
    }

    public CharMapPage(CharMapPage<T> pg)
    {
        if (pg.values != null)
        {
            values = new CharMapColumn<T>[CharMapBits.columnsPerPage];
            for (int i = 0; i < CharMapBits.columnsPerPage; i++)
            {
                values[i] = new CharMapColumn<T>(pg.values[i]);
            }
        }
        else
        {
            value = pg.value;
            values = null;
        }
    }

    public void operatorAssign(CharMapPage<T> pg)
    {
        if (pg.values != null)
        {
            if (values == null)
            {
                values = new CharMapColumn<T>[CharMapBits.columnsPerPage];
                for (int i = 0; i < CharMapBits.columnsPerPage; i++)
                    values[i] = new CharMapColumn<T>();
            }
            for (int i = 0; i < CharMapBits.columnsPerPage; i++)
                values[i].operatorAssign(pg.values[i]);
        }
        else
        {
            values = null;
            value = pg.value;
        }
    }

    public void swap(CharMapPage<T> pg)
    {
        CharMapColumn<T>[]? tem = values;
        values = pg.values;
        pg.values = tem;

        T temVal = value;
        value = pg.value;
        pg.value = temVal;
    }
}

public class CharMapPlane<T>
{
    public CharMapPage<T>[]? values;
    public T value = default!;

    public CharMapPlane()
    {
        values = null;
    }

    public CharMapPlane(CharMapPlane<T> pl)
    {
        if (pl.values != null)
        {
            values = new CharMapPage<T>[CharMapBits.pagesPerPlane];
            for (int i = 0; i < CharMapBits.pagesPerPlane; i++)
            {
                values[i] = new CharMapPage<T>(pl.values[i]);
            }
        }
        else
        {
            value = pl.value;
            values = null;
        }
    }

    public void operatorAssign(CharMapPlane<T> pl)
    {
        if (pl.values != null)
        {
            if (values == null)
            {
                values = new CharMapPage<T>[CharMapBits.pagesPerPlane];
                for (int i = 0; i < CharMapBits.pagesPerPlane; i++)
                    values[i] = new CharMapPage<T>();
            }
            for (int i = 0; i < CharMapBits.pagesPerPlane; i++)
                values[i].operatorAssign(pl.values[i]);
        }
        else
        {
            values = null;
            value = pl.value;
        }
    }

    public void swap(CharMapPlane<T> pl)
    {
        CharMapPage<T>[]? tem = values;
        values = pl.values;
        pl.values = tem;

        T temVal = value;
        value = pl.value;
        pl.value = temVal;
    }
}

public class CharMap<T>
{
    private CharMapPlane<T>[] values_ = new CharMapPlane<T>[CharMapBits.planes];
    private T[] lo_ = new T[256];

    public CharMap()
    {
        for (int i = 0; i < CharMapBits.planes; i++)
            values_[i] = new CharMapPlane<T>();
    }

    public CharMap(T dflt)
    {
        for (int i = 0; i < 256; i++)
            lo_[i] = dflt;
        for (int i = 0; i < CharMapBits.planes; i++)
        {
            values_[i] = new CharMapPlane<T>();
            values_[i].value = dflt;
        }
    }

    // T operator[](Char) const;
    public T this[Char c]
    {
        get
        {
            if (c < 256)
                return lo_[c];
            CharMapPlane<T> pl = values_[CharMapBits.planeIndex(c)];
            if (pl.values != null)
            {
                CharMapPage<T> pg = pl.values[CharMapBits.pageIndex(c)];
                if (pg.values != null)
                {
                    CharMapColumn<T> column = pg.values[CharMapBits.columnIndex(c)];
                    if (column.values != null)
                        return column.values[CharMapBits.cellIndex(c)];
                    else
                        return column.value;
                }
                else
                    return pg.value;
            }
            else
                return pl.value;
        }
    }

    // T getRange(Char from, Char &to) const;
    public T getRange(Char c, out Char max)
    {
        if (c < 256)
        {
            max = c;
            return lo_[c];
        }
        CharMapPlane<T> pl = values_[CharMapBits.planeIndex(c)];
        if (pl.values != null)
        {
            CharMapPage<T> pg = pl.values[CharMapBits.pageIndex(c)];
            if (pg.values != null)
            {
                CharMapColumn<T> column = pg.values[CharMapBits.columnIndex(c)];
                if (column.values != null)
                {
                    max = c;
                    return column.values[CharMapBits.cellIndex(c)];
                }
                else
                {
                    max = (Char)CharMapBits.maxInColumn(c);
                    return column.value;
                }
            }
            else
            {
                max = (Char)CharMapBits.maxInPage(c);
                return pg.value;
            }
        }
        else
        {
            max = (Char)CharMapBits.maxInPlane(c);
            return pl.value;
        }
    }

    // void swap(CharMap<T> &);
    public void swap(CharMap<T> map)
    {
        for (int i = 0; i < 256; i++)
        {
            T tem = lo_[i];
            lo_[i] = map.lo_[i];
            map.lo_[i] = tem;
        }
        for (int i = 0; i < CharMapBits.planes; i++)
            values_[i].swap(map.values_[i]);
    }

    // void setChar(Char, T);
    public void setChar(Char c, T val)
    {
        if (c < 256)
        {
            lo_[c] = val;
            return;
        }
        CharMapPlane<T> pl = values_[CharMapBits.planeIndex(c)];
        if (pl.values != null)
        {
            CharMapPage<T> pg = pl.values[CharMapBits.pageIndex(c)];
            if (pg.values != null)
            {
                CharMapColumn<T> column = pg.values[CharMapBits.columnIndex(c)];
                if (column.values != null)
                    column.values[CharMapBits.cellIndex(c)] = val;
                else if (!val!.Equals(column.value))
                {
                    column.values = new T[CharMapBits.columnSize];
                    for (int i = 0; i < CharMapBits.columnSize; i++)
                        column.values[i] = column.value;
                    column.values[CharMapBits.cellIndex(c)] = val;
                }
            }
            else if (!val!.Equals(pg.value))
            {
                pg.values = new CharMapColumn<T>[CharMapBits.columnsPerPage];
                for (int i = 0; i < CharMapBits.columnsPerPage; i++)
                {
                    pg.values[i] = new CharMapColumn<T>();
                    pg.values[i].value = pg.value;
                }
                CharMapColumn<T> column = pg.values[CharMapBits.columnIndex(c)];
                column.values = new T[CharMapBits.cellsPerColumn];
                for (int i = 0; i < CharMapBits.cellsPerColumn; i++)
                    column.values[i] = column.value;
                column.values[CharMapBits.cellIndex(c)] = val;
            }
        }
        else if (!val!.Equals(pl.value))
        {
            pl.values = new CharMapPage<T>[CharMapBits.pagesPerPlane];
            for (int i = 0; i < CharMapBits.pagesPerPlane; i++)
            {
                pl.values[i] = new CharMapPage<T>();
                pl.values[i].value = pl.value;
            }
            CharMapPage<T> page = pl.values[CharMapBits.pageIndex(c)];
            page.values = new CharMapColumn<T>[CharMapBits.columnsPerPage];
            for (int i = 0; i < CharMapBits.columnsPerPage; i++)
            {
                page.values[i] = new CharMapColumn<T>();
                page.values[i].value = page.value;
            }
            CharMapColumn<T> col = page.values[CharMapBits.columnIndex(c)];
            col.values = new T[CharMapBits.cellsPerColumn];
            for (int i = 0; i < CharMapBits.cellsPerColumn; i++)
                col.values[i] = col.value;
            col.values[CharMapBits.cellIndex(c)] = val;
        }
    }

    // void setRange(Char from, Char to, T val);
    public void setRange(Char from, Char to, T val)
    {
        for (; from < 256; from++)
        {
            lo_[from] = val;
            if (from == to)
                return;
        }
        do
        {
            if ((from & (CharMapBits.columnSize - 1)) == 0
                && to - from >= CharMapBits.columnSize - 1)
            {
                if ((from & (CharMapBits.pageSize - 1)) == 0
                    && to - from >= CharMapBits.pageSize - 1)
                {
                    if ((from & (CharMapBits.planeSize - 1)) == 0
                        && to - from >= CharMapBits.planeSize - 1)
                    {
                        // Set a complete plane.
                        CharMapPlane<T> pl = values_[CharMapBits.planeIndex(from)];
                        pl.value = val;
                        pl.values = null;
                        from += CharMapBits.planeSize - 1;
                    }
                    else
                    {
                        // Set a complete page.
                        CharMapPlane<T> pl = values_[CharMapBits.planeIndex(from)];
                        if (pl.values != null)
                        {
                            CharMapPage<T> pg = pl.values[CharMapBits.pageIndex(from)];
                            pg.value = val;
                            pg.values = null;
                        }
                        else if (!val!.Equals(pl.value))
                        {
                            // split the plane
                            pl.values = new CharMapPage<T>[CharMapBits.pagesPerPlane];
                            for (int i = 0; i < CharMapBits.pagesPerPlane; i++)
                            {
                                pl.values[i] = new CharMapPage<T>();
                                pl.values[i].value = pl.value;
                            }
                            CharMapPage<T> page = pl.values[CharMapBits.pageIndex(from)];
                            page.value = val;
                        }
                        from += CharMapBits.pageSize - 1;
                    }
                }
                else
                {
                    // Set a complete column.
                    CharMapPlane<T> pl = values_[CharMapBits.planeIndex(from)];
                    if (pl.values != null)
                    {
                        CharMapPage<T> pg = pl.values[CharMapBits.pageIndex(from)];
                        if (pg.values != null)
                        {
                            CharMapColumn<T> column = pg.values[CharMapBits.columnIndex(from)];
                            column.value = val;
                            column.values = null;
                        }
                        else if (!val!.Equals(pg.value))
                        {
                            // split the page
                            pg.values = new CharMapColumn<T>[CharMapBits.columnsPerPage];
                            for (int i = 0; i < CharMapBits.columnsPerPage; i++)
                            {
                                pg.values[i] = new CharMapColumn<T>();
                                pg.values[i].value = pg.value;
                            }
                            CharMapColumn<T> column = pg.values[CharMapBits.columnIndex(from)];
                            column.value = val;
                        }
                    }
                    else if (!val!.Equals(pl.value))
                    {
                        // split the plane
                        pl.values = new CharMapPage<T>[CharMapBits.pagesPerPlane];
                        for (int i = 0; i < CharMapBits.pagesPerPlane; i++)
                        {
                            pl.values[i] = new CharMapPage<T>();
                            pl.values[i].value = pl.value;
                        }
                        CharMapPage<T> pg = pl.values[CharMapBits.pageIndex(from)];
                        pg.value = val;
                        // split the page
                        pg.values = new CharMapColumn<T>[CharMapBits.columnsPerPage];
                        for (int i = 0; i < CharMapBits.columnsPerPage; i++)
                        {
                            pg.values[i] = new CharMapColumn<T>();
                            pg.values[i].value = pg.value;
                        }
                        CharMapColumn<T> column = pg.values[CharMapBits.columnIndex(from)];
                        column.value = val;
                    }
                    from += CharMapBits.columnSize - 1;
                }
            }
            else
                setChar(from, val);
        } while (from++ != to);
    }

    // void setAll(T);
    public void setAll(T val)
    {
        for (int i = 0; i < 256; i++)
            lo_[i] = val;
        for (int i = 0; i < CharMapBits.planes; i++)
        {
            values_[i].value = val;
            values_[i].values = null;
        }
    }
}

// Note: C# doesn't support multiple inheritance of classes.
// CharMap<T> and Resource are both classes, so CharMapResource inherits from CharMap<T>
// and implements IResource interface with composition.
public class CharMapResource<T> : CharMap<T>, IResource
{
    private Resource resource_ = new Resource();

    // CharMapResource() { }
    public CharMapResource() : base()
    {
    }

    // CharMapResource(T t) : CharMap<T>(t) { }
    public CharMapResource(T t) : base(t)
    {
    }

    // IResource implementation - delegated to internal Resource instance
    public int count()
    {
        return resource_.count();
    }

    public int unref()
    {
        return resource_.unref();
    }

    public void @ref()
    {
        resource_.@ref();
    }
}

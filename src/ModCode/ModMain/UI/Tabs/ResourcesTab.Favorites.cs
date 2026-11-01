using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭의 즐겨찾기 쪽(13 문서 S1로 나눔): 즐겨찾기 줄 만들기·거르기·☆ 넣고 빼기·수량 저장·모두 지급.
    internal sealed partial class ResourcesTab
    {
        private static ItemCatalog.Entry Missing(int id)
        {
            return new ItemCatalog.Entry
            {
                Id = id,
                Name = "게임에 없는 아이템 (" + id + ")",
                Desc = "즐겨찾기에 남아 있지만 지금 게임의 아이템 표에 없습니다(그 아이템을 넣은 모드를 뺐을 수 있습니다). ★를 누르면 뺍니다.",
                Search = "",
                Missing = true,
            };
        }

        private Row FavRow(FavoriteStore.Favorite favorite)
        {
            if (!favorite.Book)
            {
                return new Row { Item = ItemCatalog.Get(favorite.Id) ?? Missing(favorite.Id) };
            }
            return new Row { Book = favorite, SkillKind = favorite.BookKind, Skill = SkillCatalog.Get(favorite.BookKind, favorite.Id) };
        }

        // 즐겨찾기 줄의 거르기. 비급 줄: 검색은 기술 이름, 품질은 최소 품질이 같은 것, 보유 중만은 가방에 그 기술의 비급이 있는 것.
        // 게임에 없는 아이템·기술은 늘 보인다(뺄 수 있게).
        private bool FavPasses(Row row, HashSet<int> owned)
        {
            if (row.Item != null)
            {
                return ItemCatalog.Passes(row.Item, filter, owned);
            }
            if (row.Skill == null)
            {
                return true;
            }
            if (filter.Quality > 0 && row.Book.MinLevel != filter.Quality)
            {
                return false;
            }
            if (filter.OwnedOnly && BagBooks(row.Book.Id) == 0)
            {
                return false;
            }
            return filter.Needle.Length == 0 || row.Skill.Search.Contains(filter.Needle) || (filter.IsId && row.Skill.BaseId == filter.Id);
        }

        // 보이는(걸러진) 즐겨찾기 줄. 한 번 그리는 동안 목록과 "★ 즐겨찾기 n"이 함께 쓰므로 Draw·OnQuery가 한 번만 만든다(13 문서 P1).
        private List<Row> FavRows(HashSet<int> owned)
        {
            var rows = new List<Row>();
            foreach (FavoriteStore.Favorite favorite in FavoriteStore.Entries)
            {
                Row row = FavRow(favorite);
                if (FavPasses(row, owned))
                {
                    rows.Add(row);
                }
            }
            return rows;
        }

        private int FavCount(HashSet<int> owned)
        {
            return favRowsNow != null ? favRowsNow.Count : FavRows(owned).Count;
        }

        private List<Row> favRowsNow;   // 그리기(검색어 변경) 한 번 동안의 즐겨찾기 줄

        private string NameOf(Row row)
        {
            if (row.Item != null)
            {
                return row.Item.Name;
            }
            return row.Book != null ? BookFavName(row) : row.Skill.Name;
        }

        private void ToggleFavorite(int id)
        {
            string text;
            if (FavoriteStore.Has(id))
            {
                FavoriteStore.Remove(id, out text);
            }
            else
            {
                FavoriteStore.Add(id, AmountBar.Get("item"), out text);
            }
            Say(text);
        }

        // 저장했으면 참. 같은 수량이면 아무것도 하지 않는다(칸을 떠날 때마다 오므로 결과 칸을 채우지 않는다).
        private bool SetAmount(int id, int amount)
        {
            if (amount == FavoriteStore.AmountOf(id))
            {
                return false;
            }
            string text;
            bool ok = FavoriteStore.SetAmount(id, amount, out text);
            Say(text);
            return ok;
        }

        // 보이는(걸러진) 즐겨찾기 가운데 게임에 있는 것을 저장된 수량으로 차례로 준다. 비급 줄은 저장된 권수를 모두 넣었을 때 넣음이다.
        // 줄마다 예외를 막는다. 밖으로 나가면 버튼 콜백에서 사라져 결과 문장 없이 일부만 들어가고, 다시 누르면 앞의 것을 또 준다.
        private void GiveAll()
        {
            int given = 0;
            var failed = new List<string>();
            foreach (Row row in Rows(ItemCatalog.OwnedIds(Player)))
            {
                if (row.Item != null ? row.Item.Missing : row.Skill == null)
                {
                    continue;
                }
                string name = row.Item != null ? row.Item.Name : row.Skill.Name + " 비급";
                try
                {
                    string text;
                    bool ok = row.Item != null
                        ? EditService.TryGiveItem(Player, row.Item.Id, FavoriteStore.AmountOf(row.Item.Id), out text)
                        : MartialBookService.Make(Player, row.Book.BookKind, row.Skill, row.Book.Grade, row.Book.MinLevel, row.Book.Amount, out text) == row.Book.Amount;
                    if (ok)
                    {
                        given++;
                    }
                    else
                    {
                        failed.Add(name);
                    }
                }
                catch (Exception e)
                {
                    failed.Add(name + " — " + e.GetType().Name);
                }
            }
            Say(given + failed.Count == 0 ? "즐겨찾기 모두 지급: 줄 수 있는 아이템이 없습니다."
                : "즐겨찾기 모두 지급: 넣음 " + given + ", 못 넣음 " + failed.Count + (failed.Count > 0 ? "(" + string.Join(", ", failed.ToArray()) + ")" : "") + ".");
        }
    }
}
